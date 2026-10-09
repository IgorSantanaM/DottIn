using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using DottIn.Application.Features.Branches.Commands.CreateBranch;
using DottIn.Application.Features.Branches.DTOs;
using DottIn.Application.Interfaces;
using DottIn.Application.Shared.DTOS;
using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using DottIn.Domain.Subscriptions;
using DottIn.Domain.ValueObjects;
using DottIn.Infra.CrossCutting.IoC;
using DottIn.Infra.Data.Contexts;
using DottIn.Infra.Services.Auth;
using DottIn.Presentation.WebApi.DTOs.Branches;
using DottIn.Presentation.WebApi.Endpoints;
using DottIn.Presentation.WebApi.Middlewares;
using DottIn.Presentation.WebApi.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

namespace DottIn.WebApi.IntegrationTests.Branches;

public sealed class BranchCreationFlowTests
{
    private const string Secret = "isolated-branch-test-secret-at-least-32-characters";

    [Fact]
    [Trait("Category", "Docker")]
    public async Task OnlyPersistedOwnersCreateBranchesWithoutNewSubscriptionOrStripeCalls()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var system = await TestSystem.CreateAsync(3, ct);
        using var client = system.Client(system.Owner);
        using (var anonymous = system.App.GetTestClient())
        using (var denied = await anonymous.GetAsync("/api/branches/management", ct))
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);

        foreach (var role in new[] { EmployeeRole.Employee, EmployeeRole.Manager, EmployeeRole.Administrator })
        {
            using var scope = system.App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DottInContext>();
            var employee = new Employee($"Test {role}", new Document("11122233396"), system.Headquarters.Id,
                new TimeOnly(8, 0), new TimeOnly(17, 0), new TimeOnly(12, 0), new TimeOnly(13, 0));
            employee.SetRole(role);
            db.Add(employee);
            await db.SaveChangesAsync(ct);
            using var restricted = system.Client(employee);
            using (var denied = await restricted.GetAsync("/api/branches/management", ct))
                Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            using (var denied = await restricted.PostAsJsonAsync("/api/branches", Request("04252011000110"), ct))
                Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            // Even a validly signed Owner claim must not replace persisted ownership/role.
            using var forged = system.Client(employee, role: "Owner");
            using var rejected = await forged.PostAsJsonAsync("/api/branches", Request("04252011000110"), ct);
            Assert.Equal(HttpStatusCode.Forbidden, rejected.StatusCode);
            db.Remove(employee);
            await db.SaveChangesAsync(ct);
        }

        using (var scope = system.App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DottInContext>();
            var otherOwner = new Employee("Other owner", new Document("12345678909"), "BranchTest123!");
            db.Add(otherOwner);
            await db.SaveChangesAsync(ct);
            using var outsider = system.Client(otherOwner);
            using var denied = await outsider.GetAsync("/api/branches/management", ct);
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        }

        using (var invalid = await client.PostAsJsonAsync("/api/branches", Request("04252011000110") with
                   { Geolocation = new GeolocationDto(0, 0) }, ct))
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var request = Request("04252011000110") with
        { OwnerId = Guid.NewGuid(), IsHeadQuarters = true, CreatedByEmployeeId = Guid.NewGuid() };
        using var createdResponse = await client.PostAsJsonAsync("/api/branches", request, ct);
        Assert.True(createdResponse.StatusCode == HttpStatusCode.Created, await createdResponse.Content.ReadAsStringAsync(ct));
        var created = (await createdResponse.Content.ReadFromJsonAsync<CreatedBranch>(ct))!;
        using (var scope = system.App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DottInContext>();
            var branch = await db.Branches.AsNoTracking().SingleAsync(b => b.Id == created.BranchId, ct);
            Assert.Equal(system.Owner.Id, branch.OwnerId);
            Assert.Equal(system.Owner.Id, branch.CreatedByEmployeeId);
            Assert.False(branch.IsHeadquarters);
            Assert.Equal("America/Manaus", branch.TimeZoneId);
            Assert.Equal(250, branch.AllowedRadiusMeters);
            Assert.Equal(15, branch.ToleranceMinutes);
            Assert.Equal(1, await db.TenantSubscriptions.CountAsync(ct));
            Assert.NotEmpty(branch.CompanyCode);
        }
        using (var duplicate = await client.PostAsJsonAsync("/api/branches", request, ct))
            Assert.Equal(HttpStatusCode.UnprocessableEntity, duplicate.StatusCode);

        var management = (await client.GetFromJsonAsync<BranchManagementResponse>("/api/branches/management", ct))!;
        Assert.Equal(system.Owner.Id, management.OwnerId);
        Assert.Equal(2, management.Subscription!.CurrentBranchCount);
        Assert.Single(management.Branches, b => b.IsHeadquarters);
        Assert.Contains(management.Branches, b => b.Id == created.BranchId);

        using (var scope = system.App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DottInContext>();
            var subscription = await db.TenantSubscriptions.SingleAsync(ct);
            subscription.MarkPastDue();
            await db.SaveChangesAsync(ct);
        }
        using (var ineligible = await client.PostAsJsonAsync("/api/branches", Request("11444777000161"), ct))
        {
            Assert.Equal(HttpStatusCode.Conflict, ineligible.StatusCode);
            Assert.Contains("assinatura", await ineligible.Content.ReadAsStringAsync(ct));
        }
        using (var scope = system.App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DottInContext>();
            (await db.TenantSubscriptions.SingleAsync(ct)).MarkActive();
            await db.SaveChangesAsync(ct);
        }
        using (var lastSlot = await client.PostAsJsonAsync("/api/branches", Request("11444777000161"), ct))
            Assert.Equal(HttpStatusCode.Created, lastSlot.StatusCode);
        using (var full = await client.PostAsJsonAsync("/api/branches", Request("19131243000197"), ct))
        {
            Assert.Equal(HttpStatusCode.Conflict, full.StatusCode);
            Assert.Contains("limite", await full.Content.ReadAsStringAsync(ct));
        }
        Assert.Equal(0, system.Stripe.CustomersCreated);

        using (var scope = system.App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DottInContext>();
            var inactive = new Branch("Inactive", new("19131243000197"), new(-15.6, -56.1),
                new("Rua Teste", 1, "Cuiabá", "MT", "78000000"), "America/Cuiaba", new(8, 0), new(17, 0), system.Owner.Id, email: "test@example.invalid");
            inactive.Deactivate();
            db.Add(inactive);
            await db.SaveChangesAsync(ct);
            using var overQuota = await client.PatchAsync($"/api/branches/{inactive.Id}/activate", null, ct);
            Assert.Equal(HttpStatusCode.Conflict, overQuota.StatusCode);
        }
    }

    [Fact]
    [Trait("Category", "Docker")]
    public async Task SubscriptionOwnerCanCreateForTheSameHeadquartersAndInitialOnboardingStillWorks()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var system = await TestSystem.CreateAsync(3, ct);
        var payer = new Employee("Subscription owner", new Document("12345678909"), "BranchTest123!");
        using (var scope = system.App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DottInContext>();
            db.Add(payer);
            await db.SaveChangesAsync(ct);
            await db.TenantSubscriptions.ExecuteUpdateAsync(s => s.SetProperty(x => x.OwnerId, payer.Id), ct);
        }
        using var payerClient = system.Client(payer);
        using (var listing = await payerClient.GetAsync("/api/branches/management", ct))
            Assert.Equal(HttpStatusCode.OK, listing.StatusCode);
        using (var created = await payerClient.PostAsJsonAsync("/api/branches", Request("04252011000110"), ct))
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using (var scope = system.App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DottInContext>();
            var branch = await db.Branches.SingleAsync(b => !b.IsHeadquarters, ct);
            Assert.Equal(system.Owner.Id, branch.OwnerId);
            Assert.Equal(payer.Id, branch.CreatedByEmployeeId);
            Assert.Equal(1, await db.TenantSubscriptions.CountAsync(ct));
        }
        var management = (await payerClient.GetFromJsonAsync<BranchManagementResponse>("/api/branches/management", ct))!;
        Assert.Equal(2, management.Subscription!.CurrentBranchCount);
        Assert.Equal(0, system.Stripe.CustomersCreated);

        var newcomer = new Employee("New owner", new Document("10111213100"), "BranchTest123!");
        using (var scope = system.App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DottInContext>();
            db.Add(newcomer);
            await db.SaveChangesAsync(ct);
        }
        using var onboarding = system.Client(newcomer, tenantId: newcomer.Id);
        using (var first = await onboarding.PostAsJsonAsync("/api/branches", Request("11444777000161"), ct))
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        using (var extra = await onboarding.PostAsJsonAsync("/api/branches", Request("19131243000197"), ct))
            Assert.Equal(HttpStatusCode.Conflict, extra.StatusCode); // Free includes only the headquarters.
        using (var scope = system.App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DottInContext>();
            var firstBranch = await db.Branches.SingleAsync(b => b.OwnerId == newcomer.Id, ct);
            Assert.True(firstBranch.IsHeadquarters);
            Assert.Equal(firstBranch.Id, (await db.Employees.AsNoTracking().SingleAsync(e => e.Id == newcomer.Id, ct)).BranchId);
            Assert.Equal(1, await db.TenantSubscriptions.CountAsync(s => s.OwnerId == newcomer.Id, ct));
        }
        Assert.Equal(1, system.Stripe.CustomersCreated);
    }

    [Fact]
    [Trait("Category", "Docker")]
    public async Task ConcurrentRequestsCannotExceedQuotaOrCreateDuplicateDocument()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var system = await TestSystem.CreateAsync(2, ct);
        using var client = system.Client(system.Owner);
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/api/branches", Request("04252011000110"), ct),
            client.PostAsJsonAsync("/api/branches", Request("11444777000161"), ct));
        Assert.True(responses.Any(r => r.StatusCode == HttpStatusCode.Created), string.Join("\n", await Task.WhenAll(responses.Select(r => r.Content.ReadAsStringAsync(ct)))));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        foreach (var response in responses) response.Dispose();

        using var scope = system.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DottInContext>();
        Assert.Equal(2, await db.Branches.CountAsync(b => b.IsActive, ct));
        var branch = await db.Branches.SingleAsync(b => !b.IsHeadquarters, ct);
        var document = branch.Document.Value;
        branch.Deactivate();
        await db.SaveChangesAsync(ct);
        var duplicates = await Task.WhenAll(
            client.PostAsJsonAsync("/api/branches", Request(document), ct),
            client.PostAsJsonAsync("/api/branches", Request(document), ct));
        Assert.All(duplicates, r => Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode));
        foreach (var response in duplicates) response.Dispose();

        // Two activations of the same unit share one quota slot.
        var activations = await Task.WhenAll(client.PatchAsync($"/api/branches/{branch.Id}/activate", null, ct),
            client.PatchAsync($"/api/branches/{branch.Id}/activate", null, ct));
        Assert.All(activations, r => Assert.Equal(HttpStatusCode.NoContent, r.StatusCode));
        foreach (var response in activations) response.Dispose();
        Assert.Equal(2, await db.Branches.CountAsync(b => b.IsActive, ct));
    }

    private static CreateBranchCommand Request(string cnpj) => new("Filial de teste", new(cnpj, DocumentType.CNPJ),
        new(-15.6, -56.1), new("Rua Teste", 10, null, "Cuiabá", "MT", "78000000"),
        "America/Manaus", new(8, 0), new(17, 0), "branch@example.invalid", "65999999999", null, false, 250, 15);

    private sealed record CreatedBranch(Guid BranchId, string CompanyCode);

    private sealed class TestSystem(PostgreSqlContainer postgres, WebApplication app, Employee owner, Branch headquarters, NoBillingStripe stripe) : IAsyncDisposable
    {
        public WebApplication App => app;
        public Employee Owner => owner;
        public Branch Headquarters => headquarters;
        public NoBillingStripe Stripe => stripe;
        public HttpClient Client(Employee employee, string? role = null, Guid? tenantId = null)
        {
            var client = app.GetTestClient();
            using var scope = app.Services.CreateScope();
            var token = scope.ServiceProvider.GetRequiredService<ITokenService>().GenerateToken(employee.Id, employee.BranchId,
                tenantId ?? owner.Id, role ?? employee.Role.ToString(), Secret, "BranchTests", "BranchTests", 15, employee.SessionVersion);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }
        public static async Task<TestSystem> CreateAsync(int maxBranches, CancellationToken ct)
        {
            var postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await postgres.StartAsync(ct);
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
            builder.WebHost.UseTestServer();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            { ["ConnectionStrings:DottInDb"] = postgres.GetConnectionString(), ["MassTransit:Disabled"] = "true" });
            builder.Services.RegisterApplication(builder.Configuration);
            builder.Services.RegisterInfrastructure(builder.Configuration);
            builder.Services.AddMassTransitConfiguration(builder.Configuration);
            var stripe = new NoBillingStripe();
            builder.Services.AddSingleton<IStripeService>(stripe);
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<CurrentUserContext>();
            builder.Services.AddScoped<TenantAccessService>();
            builder.Services.AddScoped<TenantAuthorizationFilter>();
            builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
                o.TokenValidationParameters = new TokenValidationParameters
                { ValidIssuer = "BranchTests", ValidAudience = "BranchTests", IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)) });
            builder.Services.AddAuthorization();
            var app = builder.Build();
            app.UseMiddleware<ErrorHandlingMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();
            BranchEndpoints.DefineEndpoints(app);
            await app.StartAsync(ct);
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DottInContext>();
            await db.Database.MigrateAsync(ct);
            var owner = new Employee("Owner", new Document("10020030088"), "BranchTest123!");
            var headquarters = new Branch("Matriz", new("11222333000181"), new(-15.6, -56.1), new("Rua Matriz", 1, "Cuiabá", "MT", "78000000"),
                "America/Cuiaba", new(8, 0), new(17, 0), owner.Id, email: "hq@example.invalid", isHeadquarters: true);
            var plan = new SubscriptionPlan("Branch test", 10, maxBranches, 0);
            db.AddRange(owner, headquarters, plan, new TenantSubscription(headquarters.Id, owner.Id, "test-customer", plan.Id));
            await db.SaveChangesAsync(ct);
            return new TestSystem(postgres, app, owner, headquarters, stripe);
        }
        public async ValueTask DisposeAsync() { await app.DisposeAsync(); await postgres.DisposeAsync(); }
    }

    private sealed class NoBillingStripe : IStripeService
    {
        public int CustomersCreated;
        public Task<string> CreateCustomerAsync(string email, string name, Guid headquartersId, CancellationToken cancellationToken = default)
        { Interlocked.Increment(ref CustomersCreated); return Task.FromResult("test-new-customer"); }
        public Task<string> CreateCheckoutSessionAsync(string customerId, string priceId, Guid headquartersId, CancellationToken cancellationToken = default) => throw new InvalidOperationException("No checkout allowed");
        public Task<string> CreateCustomerPortalSessionAsync(string customerId, CancellationToken cancellationToken = default) => throw new InvalidOperationException("No portal allowed");
        public Task CancelSubscriptionAsync(string subscriptionId, bool cancelImmediately = false, CancellationToken cancellationToken = default) => throw new InvalidOperationException("No cancellation allowed");
        public Task<StripeSubscriptionInfo?> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default) => throw new InvalidOperationException("No Stripe lookup allowed");
        public StripeWebhookEvent? ParseWebhookEvent(string json, string signature) => throw new InvalidOperationException("No webhook allowed");
    }
}
