using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using DottIn.Domain.Auth;
using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using DottIn.Domain.HolidayCalendars;
using DottIn.Domain.ValueObjects;
using DottIn.Infra.Data.Contexts;
using DottIn.Infra.Services.Auth;
using DottIn.Presentation.WebApi.DTOs.Settings;
using DottIn.Presentation.WebApi.Endpoints;
using DottIn.Presentation.WebApi.Middlewares;
using DottIn.Presentation.WebApi.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

namespace DottIn.WebApi.IntegrationTests.Settings;

[Trait("Category", "Docker")]
public sealed class SettingsApiFlowTests
{
    [Fact]
    public async Task AdministrativeSettingsRejectOtherRolesAndOtherTenants()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var system = await SystemFixture.CreateAsync(ct);
        foreach (var role in new[] { EmployeeRole.Employee, EmployeeRole.Manager, EmployeeRole.Accountant, EmployeeRole.Administrator })
        {
            var employee = new Employee("Test", new Document("11122233396"), system.Branch.Id, new(8, 0), new(17, 0), new(12, 0), new(13, 0));
            employee.SetRole(role);
            using var scope = system.App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DottInContext>();
            db.Add(employee); await db.SaveChangesAsync(ct);
            using var client = system.Client(employee);
            Assert.Equal(role == EmployeeRole.Administrator ? HttpStatusCode.OK : HttpStatusCode.Forbidden,
                (await client.GetAsync($"/api/settings/branches/{system.Branch.Id}", ct)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/settings/me", ct)).StatusCode);
            using var forged = system.Client(employee, role: "Owner");
            if (role is not EmployeeRole.Administrator)
                Assert.Equal(HttpStatusCode.Forbidden, (await forged.GetAsync($"/api/settings/branches/{system.Branch.Id}", ct)).StatusCode);
            db.Remove(employee); await db.SaveChangesAsync(ct);
        }
        using var owner = system.Client(system.Owner);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync($"/api/settings/branches/{system.OtherBranch.Id}", ct)).StatusCode);
    }

    [Fact]
    public async Task ConcurrentSettingsSaveHasOneWinnerAndOneAuditWithPersistedVersion()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var system = await SystemFixture.CreateAsync(ct);
        using var client = system.Client(system.Owner);
        var url = $"/api/settings/branches/{system.Branch.Id}";
        var initial = (await client.GetFromJsonAsync<UnitSettings>(url, ct))!;
        var responses = await Task.WhenAll(
            client.PutAsJsonAsync(url, new UpdateUnitSettings("rules", initial.Version, AllowedRadiusMeters: 150, ToleranceMinutes: 15), ct),
            client.PutAsJsonAsync(url, new UpdateUnitSettings("rules", initial.Version, AllowedRadiusMeters: 200, ToleranceMinutes: 20), ct));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        var saved = (await responses.Single(r => r.IsSuccessStatusCode).Content.ReadFromJsonAsync<UnitSettings>(ct))!;
        var reloaded = (await client.GetFromJsonAsync<UnitSettings>(url, ct))!;
        Assert.Equal(saved.Version, reloaded.Version);
        using var scope = system.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DottInContext>();
        var audit = await db.BranchSettingsAudits.SingleAsync(ct);
        Assert.Equal(system.Owner.Id, audit.EmployeeId);
        Assert.Equal(system.Branch.Id, audit.BranchId);
        Assert.Equal("rules", audit.Section);
        Assert.DoesNotContain("Password", audit.AfterJson);
        var foreignCalendar = new HolidayCalendar(system.OtherBranch.Id, "Foreign", "BR", 2026);
        db.Add(foreignCalendar); await db.SaveChangesAsync(ct);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(url,
            new UpdateUnitSettings("rules", reloaded.Version, AllowedRadiusMeters: 100, ToleranceMinutes: 10, HolidayCalendarId: foreignCalendar.Id), ct)).StatusCode);
        Assert.Equal(1, await db.BranchSettingsAudits.CountAsync(ct));
        var next = await client.PutAsJsonAsync(url, new UpdateUnitSettings("schedule", reloaded.Version, StartWork: new(9, 0), EndWork: new(18, 0)), ct);
        Assert.Equal(HttpStatusCode.OK, next.StatusCode); // The version returned by a save remains usable after a reload.
    }

    [Fact]
    public async Task OwnProfileCannotChangeRoleAndCredentialsWorkBeforeCompanySetup()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var system = await SystemFixture.CreateAsync(ct);
        using var client = system.Client(system.Owner);
        var profile = (await client.GetFromJsonAsync<PersonalSettings>("/api/settings/me", ct))!;
        var response = await client.PutAsJsonAsync("/api/settings/me", new { Name = "Updated owner", profile.Version, Role = "Employee", EmployeeId = Guid.NewGuid() }, ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<PersonalSettings>(ct))!;
        Assert.Equal(updated.Version, (await client.GetFromJsonAsync<PersonalSettings>("/api/settings/me", ct))!.Version);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync("/api/settings/me", new UpdatePersonalSettings("Stale", profile.Version), ct)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/settings/password", new ChangeSettingsCredential("WrongPassword!", "NewStrongPass123!"), ct)).StatusCode);
        using var scope = system.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DottInContext>();
        db.RefreshTokens.Add(new(system.Owner.Id, Guid.Empty, system.Owner.SessionVersion));
        await db.SaveChangesAsync(ct);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync("/api/settings/password", new ChangeSettingsCredential("StrongPass123!", "NewStrongPass123!"), ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/settings/me", ct)).StatusCode);
        db.ChangeTracker.Clear();
        var persisted = await db.Employees.SingleAsync(e => e.Id == system.Owner.Id, ct);
        Assert.Equal(EmployeeRole.Owner, persisted.Role);
        Assert.Equal(Guid.Empty, persisted.BranchId);
        Assert.True(persisted.VerifyPassword("NewStrongPass123!"));
        Assert.Empty(await db.RefreshTokens.ToListAsync(ct));
    }

    private sealed class SystemFixture(PostgreSqlContainer postgres, WebApplication app, Employee owner, Branch branch, Branch other) : IAsyncDisposable
    {
        private const string Secret = "isolated-settings-tests-secret-at-least-32-characters";
        public WebApplication App => app;
        public Employee Owner => owner;
        public Branch Branch => branch;
        public Branch OtherBranch => other;
        public HttpClient Client(Employee employee, string? role = null)
        {
            var client = app.GetTestClient();
            var token = new TokenService().GenerateToken(employee.Id, employee.BranchId, owner.Id,
                role ?? employee.Role.ToString(), Secret, "SettingsTests", "SettingsTests", 15, employee.SessionVersion);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }
        public static async Task<SystemFixture> CreateAsync(CancellationToken ct)
        {
            var postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await postgres.StartAsync(ct);
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddDbContext<DottInContext>(o => o.UseNpgsql(postgres.GetConnectionString(), p => p.EnableRetryOnFailure()));
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<CurrentUserContext>(); builder.Services.AddScoped<TenantAccessService>();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => {
                o.TokenValidationParameters = new() { ValidIssuer = "SettingsTests", ValidAudience = "SettingsTests", IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)) };
                o.Events = new JwtBearerEvents { OnTokenValidated = async context => {
                    var db = context.HttpContext.RequestServices.GetRequiredService<DottInContext>();
                    var principal = context.Principal!;
                    var id = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
                    var version = Guid.Parse(principal.FindFirstValue("sessionVersion")!);
                    if (!await db.Employees.AsNoTracking().AnyAsync(e => e.Id == id && e.IsActive && e.SessionVersion == version, context.HttpContext.RequestAborted)) context.Fail("Revoked");
                }};
            });
            builder.Services.AddAuthorization();
            builder.Services.AddRateLimiter(o => o.AddFixedWindowLimiter("public-auth", policy =>
            { policy.PermitLimit = 100; policy.Window = TimeSpan.FromMinutes(1); policy.QueueLimit = 0; }));
            var app = builder.Build();
            app.UseMiddleware<ErrorHandlingMiddleware>(); app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization();
            SettingsEndpoints.DefineEndpoints(app);
            await app.StartAsync(ct);
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DottInContext>();
            await db.Database.MigrateAsync(ct);
            var owner = new Employee("Owner", new Document("10020030088"), "StrongPass123!");
            var branch = new Branch("HQ", new("11222333000181"), new(-15, -56), new("Rua", 1, "Cuiabá", "MT", "78000000"), "America/Cuiaba", new(8, 0), new(17, 0), owner.Id, email: "test@example.invalid");
            var other = new Branch("Other", new("11444777000161"), new(-15, -56), new("Rua", 1, "Cuiabá", "MT", "78000000"), "America/Cuiaba", new(8, 0), new(17, 0), Guid.NewGuid(), email: "test@example.invalid");
            db.AddRange(owner, branch, other); await db.SaveChangesAsync(ct);
            return new(postgres, app, owner, branch, other);
        }
        public async ValueTask DisposeAsync() { await app.DisposeAsync(); await postgres.DisposeAsync(); }
    }
}
