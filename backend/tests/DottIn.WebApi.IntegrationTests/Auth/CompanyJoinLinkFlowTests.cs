using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using DottIn.Domain.Subscriptions;
using DottIn.Domain.ValueObjects;
using DottIn.Infra.CrossCutting.IoC;
using DottIn.Infra.Data.Contexts;
using DottIn.Presentation.WebApi.DTOs.Auth;
using DottIn.Presentation.WebApi.DTOs.Employees;
using DottIn.Presentation.WebApi.Endpoints;
using DottIn.Presentation.WebApi.Middlewares;
using DottIn.Presentation.WebApi.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

namespace DottIn.WebApi.IntegrationTests.Auth;

public sealed class CompanyJoinLinkFlowTests
{
    private const string Password = "InviteTest123!";
    private const string Secret = "isolated-invitation-test-secret-at-least-32-characters";

    [Fact]
    [Trait("Category", "Docker")]
    public async Task RealApiConfirmsExistingMembersWithoutTransferringBranchOrEscalatingRole()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await postgres.StartAsync(ct);
        await using var app = await CreateAppAsync(postgres.GetConnectionString(), ct);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DottInContext>();
        await db.Database.MigrateAsync(ct);

        // The owner intentionally has no assigned branch yet: their own invite
        // must not demote them to Employee or consume a seat.
        var owner = new Employee("Owner", new Document("10020030088"), Password);
        var otherOwner = new Employee("Other owner", new Document("12345678909"), Password);
        var headquarters = Branch("Matriz", "11222333000181", owner.Id, "HQ");
        var sibling = Branch("Filial", "04252011000110", owner.Id, "BRANCH");
        var otherCompany = Branch("Outra empresa", "11444777000161", otherOwner.Id, "OTHER");
        var employee = Employee("Employee", "11122233396", headquarters.Id);
        var siblingEmployee = Employee("Sibling employee", "20212223224", sibling.Id);
        var outsider = Employee("Other company employee", "88899900078", otherCompany.Id);
        var unassigned = new Employee("New member", new Document("10111213100"), Password);
        var inactive = Employee("Inactive employee", "22233344405", headquarters.Id);
        inactive.SetPin("123456");
        inactive.Deactivate();
        // Exactly two billable seats are already occupied. Existing members
        // can still use their invite when the subscription is at capacity.
        var plan = new SubscriptionPlan("Test", 2, 3, 0);
        var subscription = new TenantSubscription(headquarters.Id, owner.Id, "test-customer", plan.Id);
        var invite = new CompanyJoinLink(headquarters.Id, owner.Id, DateTime.UtcNow.AddDays(30));
        db.AddRange(owner, otherOwner, headquarters, sibling, otherCompany, employee,
            siblingEmployee, outsider, unassigned, inactive, plan, subscription, invite);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        var token = scope.ServiceProvider.GetRequiredService<ICompanyJoinLinkTokenService>().CreateToken(invite);

        using var client = app.GetTestClient();
        var anonymous = await ResolveAsync(client, token, ct);
        Assert.False(anonymous.AlreadyMember);
        Assert.False(anonymous.CanJoin);

        foreach (var member in new[] { owner, employee, siblingEmployee })
        {
            using var response = await LoginAsync(client, member.CPF.Value, token, ct);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var login = (await response.Content.ReadFromJsonAsync<LoginResponse>(ct))!;
            Assert.True(login.CompanyJoinAlreadyMember);
            Assert.Equal(member.BranchId, login.BranchId);
            Assert.Equal(member.Role.ToString(), login.Role);
            Assert.Contains(response.Headers.GetValues("Set-Cookie"), value => value.Contains("httponly", StringComparison.OrdinalIgnoreCase));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
            Assert.True((await ResolveAsync(client, token, ct)).AlreadyMember);
            var persisted = await db.Employees.AsNoTracking().SingleAsync(e => e.Id == member.Id, ct);
            Assert.Equal(member.BranchId, persisted.BranchId);
            Assert.Equal(member.Role, persisted.Role);
            Assert.Equal(member.SessionVersion, persisted.SessionVersion);
            client.DefaultRequestHeaders.Authorization = null;
        }

        using (var response = await LoginAsync(client, outsider.CPF.Value, token, ct))
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using (var response = await LoginAsync(client, employee.CPF.Value, "invalid-token", ct))
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using (var response = await LoginAsync(client, inactive.CPF.Value, token, ct))
        {
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var error = (await response.Content.ReadFromJsonAsync<AuthAccessErrorResponse>(ct))!;
            Assert.Equal("employee_inactive", error.Code);
            Assert.Contains("vínculo ativo", error.Message);
            Assert.DoesNotContain("Set-Cookie", response.Headers.Select(header => header.Key));
        }
        using (var response = await client.PostAsJsonAsync("/api/auth/login",
                   new { cpf = inactive.CPF.Value, password = "wrong-password", companyJoinToken = token }, ct))
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using (var response = await client.PostAsJsonAsync("/api/auth/login/pin",
                   new PinLoginRequest(inactive.CPF.Value, "123456", headquarters.CompanyCode), ct))
        {
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("employee_inactive", (await response.Content.ReadFromJsonAsync<AuthAccessErrorResponse>(ct))!.Code);
        }
        using (var response = await client.PostAsJsonAsync("/api/auth/login/pin",
                   new PinLoginRequest(inactive.CPF.Value, "654321", headquarters.CompanyCode), ct))
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using (var response = await LoginAsync(client, unassigned.CPF.Value, token, ct))
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        // Release a seat and exercise the original automatic join behavior.
        await db.Employees.Where(e => e.Id == siblingEmployee.Id)
            .ExecuteUpdateAsync(update => update.SetProperty(e => e.IsActive, false), ct);
        using (var response = await LoginAsync(client, unassigned.CPF.Value, token, ct))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var login = (await response.Content.ReadFromJsonAsync<LoginResponse>(ct))!;
            Assert.False(login.CompanyJoinAlreadyMember);
            Assert.Equal(headquarters.Id, login.BranchId);
            Assert.Equal("Employee", login.Role);
            Assert.False(login.IsOwner);
        }
        using (var response = await LoginAsync(client, unassigned.CPF.Value, token, ct))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True((await response.Content.ReadFromJsonAsync<LoginResponse>(ct))!.CompanyJoinAlreadyMember);
        }

        await db.Employees.Where(e => e.Id == unassigned.Id)
            .ExecuteUpdateAsync(update => update.SetProperty(e => e.IsActive, false), ct);
        using (var response = await client.PostAsJsonAsync("/api/company-join-links/register",
                   new RegisterFromCompanyJoinLinkRequest(token, "Registered employee", "77788899941", Password), ct))
        {
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var registered = (await response.Content.ReadFromJsonAsync<RegisterFromCompanyJoinLinkResponse>(ct))!;
            var persisted = await db.Employees.AsNoTracking().SingleAsync(e => e.Id == registered.EmployeeId, ct);
            Assert.Equal(EmployeeRole.Employee, persisted.Role);
            Assert.Equal(headquarters.Id, persisted.BranchId);
        }
        // Expiration/revocation is checked before membership, never bypassed.
        await db.CompanyJoinLinks.Where(link => link.Id == invite.Id)
            .ExecuteUpdateAsync(update => update.SetProperty(link => link.ExpiresAt, DateTime.UtcNow.AddDays(-1)), ct);
        using var revoked = await client.GetAsync("/api/company-join-links/resolve?token=" + Uri.EscapeDataString(token), ct);
        Assert.Equal(HttpStatusCode.BadRequest, revoked.StatusCode);
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string cpf, string token, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { cpf, password = Password, companyJoinToken = token })
        };
        request.Headers.Add("X-DottIn-Persist-Session", "true");
        return client.SendAsync(request, ct);
    }

    private static async Task<CompanyJoinLinkResolutionResponse> ResolveAsync(HttpClient client, string token, CancellationToken ct)
    {
        using var response = await client.GetAsync("/api/company-join-links/resolve?token=" + Uri.EscapeDataString(token), ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CompanyJoinLinkResolutionResponse>(ct))!;
    }

    private static Branch Branch(string name, string cnpj, Guid owner, string code) => new(name,
        new Document(cnpj), new Geolocation(-15.6, -56.1), new Address("Rua Teste", 1, "Cuiabá", "MT", "78000000"),
        "America/Cuiaba", new TimeOnly(8, 0), new TimeOnly(17, 0), owner, email: "test@example.invalid", companyCode: code);

    private static Employee Employee(string name, string cpf, Guid branch)
    {
        var employee = new Employee(name, new Document(cpf), branch,
            new TimeOnly(8, 0), new TimeOnly(17, 0), new TimeOnly(12, 0), new TimeOnly(13, 0));
        employee.SetPassword(Password);
        return employee;
    }

    private static async Task<WebApplication> CreateAppAsync(string connectionString, CancellationToken ct)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DottInDb"] = connectionString,
            ["JwtSettings:SecretKey"] = Secret,
            ["JwtSettings:Issuer"] = "InviteTests",
            ["JwtSettings:Audience"] = "InviteTests",
            ["JwtSettings:ExpirationMinutes"] = "15",
            ["MassTransit:Disabled"] = "true"
        });
        builder.Services.RegisterApplication(builder.Configuration);
        builder.Services.RegisterInfrastructure(builder.Configuration);
        builder.Services.AddMassTransitConfiguration(builder.Configuration);
        builder.Services.AddDataProtection();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<CurrentUserContext>();
        builder.Services.AddScoped<TenantAccessService>();
        builder.Services.AddScoped<TenantAuthorizationFilter>();
        builder.Services.AddScoped<ICompanyJoinLinkTokenService, CompanyJoinLinkTokenService>();
        builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("public-auth", limiter =>
        {
            limiter.PermitLimit = 1000;
            limiter.Window = TimeSpan.FromMinutes(1);
        }));
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = "InviteTests", ValidAudience = "InviteTests",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret))
            };
        });
        builder.Services.AddAuthorization();
        var app = builder.Build();
        app.UseMiddleware<ErrorHandlingMiddleware>();
        app.UseRouting();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        AuthEndpoints.DefineEndpoints(app);
        CompanyJoinLinkEndpoints.DefineEndpoints(app);
        await app.StartAsync(ct);
        return app;
    }
}
