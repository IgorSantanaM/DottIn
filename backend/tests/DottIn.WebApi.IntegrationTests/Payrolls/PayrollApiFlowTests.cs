using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using DottIn.Domain.Exports;
using DottIn.Domain.Payrolls;
using DottIn.Domain.Subscriptions;
using DottIn.Domain.TimeKeepings;
using DottIn.Domain.ValueObjects;
using DottIn.Infra.Data.Contexts;
using DottIn.Infra.Data.Repositories;
using DottIn.Infra.Services.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace DottIn.WebApi.IntegrationTests.Payrolls;

public sealed class PayrollApiFlowTests
{
    private const string Secret = "payroll-test-secret-key-with-more-than-thirty-two-characters-2026";
    private const string Issuer = "DottInPayrollTests";
    private const string Audience = "DottInPayrollTestsClient";

    [Fact]
    public async Task OwnerCloses_AccountantExports_AndOtherTenantCannotReadOrChange()
    {
        await using var postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("dottin_payroll_tests")
            .WithUsername("postgres")
            .WithPassword("test-only-password")
            .Build();
        await postgres.StartAsync();

        await using var factory = new PayrollFactory(postgres.GetConnectionString());
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DottInContext>();
        await db.Database.MigrateAsync();

        var ownerA = new Employee("Proprietário A", Cpf(100000001), "StrongPassword1!");
        var ownerB = new Employee("Proprietário B", Cpf(100000002), "StrongPassword1!");
        db.Employees.AddRange(ownerA, ownerB);
        await db.SaveChangesAsync();

        var branchA = NewBranch("Filial A", Cnpj(100000000001), ownerA.Id);
        var branchB = NewBranch("Filial B", Cnpj(100000000002), ownerB.Id);
        db.Branches.AddRange(branchA, branchB);
        await db.SaveChangesAsync();
        var freePlan = await db.SubscriptionPlans.SingleAsync(p => p.Name == "Free");
        db.TenantSubscriptions.Add(new TenantSubscription(branchA.Id, ownerA.Id, "test-customer-a", freePlan.Id));
        await db.SaveChangesAsync();

        var worker = new Employee("João da Silva", Cpf(100000003), branchA.Id,
            new TimeOnly(8, 0), new TimeOnly(17, 0), new TimeOnly(12, 0), new TimeOnly(13, 0));
        var accountant = Employee.CreateAccountant("Contador", Cpf(100000004), branchB.Id, "StrongPassword1!");
        db.Employees.AddRange(worker, accountant);
        db.DominioEmployeeMappings.Add(new DominioEmployeeMapping(worker.Id, branchA.Id, "12345"));
        var start = new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc);
        var record = new TimeKeeping(branchA.Id, worker.Id, new Geolocation(-15.6, -56.1),
            new DateOnly(2026, 10, 2), "UTC", start);
        record.ClockIn(start); record.ClockOut(start.AddHours(8));
        db.TimeKeepings.Add(record);
        await db.SaveChangesAsync();

        using var ownerClient = Client(factory, ownerA, branchA.Id, ownerA.Id);
        using var otherClient = Client(factory, ownerB, branchB.Id, ownerB.Id);
        using var accountantClient = Client(factory, accountant, branchB.Id, ownerB.Id);

        using var create = await ownerClient.PostAsJsonAsync("/api/payrolls",
            new { BranchId = branchA.Id, Year = 2026, Month = 10 });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var draft = await ReadJsonAsync(create);
        var payrollId = draft.GetProperty("id").GetGuid();
        Assert.Equal(480, draft.GetProperty("items")[0].GetProperty("workedMinutes").GetInt64());
        Assert.Equal("Draft", draft.GetProperty("status").GetString());

        using var duplicate = await ownerClient.PostAsJsonAsync("/api/payrolls",
            new { BranchId = branchA.Id, Year = 2026, Month = 10 });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await otherClient.GetAsync($"/api/payrolls/{payrollId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await accountantClient.GetAsync($"/api/payrolls/{payrollId}")).StatusCode);
        using var otherList = await otherClient.GetAsync("/api/payrolls");
        Assert.Empty((await ReadJsonAsync(otherList)).EnumerateArray());

        var version = draft.GetProperty("version").GetGuid();
        var paymentUrl = $"/api/payrolls/{payrollId}/employees/{worker.Id}/payment";
        using var deniedPayment = await otherClient.PutAsJsonAsync(paymentUrl,
            new { Amount = 9000m, Version = version });
        Assert.Equal(HttpStatusCode.NotFound, deniedPayment.StatusCode);

        using var payment = await ownerClient.PutAsJsonAsync(paymentUrl,
            new { Amount = 3500m, Notes = "Aprovado", Version = version });
        Assert.Equal(HttpStatusCode.OK, payment.StatusCode);
        var saved = await ReadJsonAsync(payment);
        Assert.Equal(3500m, saved.GetProperty("items")[0].GetProperty("paymentAmount").GetDecimal());
        using var stale = await ownerClient.PutAsJsonAsync(paymentUrl,
            new { Amount = 4000m, Version = version });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        using var close = await ownerClient.PostAsJsonAsync($"/api/payrolls/{payrollId}/close",
            new { Version = saved.GetProperty("version").GetGuid() });
        Assert.Equal(HttpStatusCode.OK, close.StatusCode);
        Assert.Equal("ReadyForAccounting", (await ReadJsonAsync(close)).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.NotFound,
            (await accountantClient.GetAsync($"/api/payrolls/{payrollId}")).StatusCode);

        using var invite = await ownerClient.PostAsJsonAsync(
            $"/api/branches/{branchA.Id}/employee-invitations",
            new { Role = "Accountant", ExpiresInHours = 72 });
        Assert.Equal(HttpStatusCode.Created, invite.StatusCode);
        var invitationToken = (await ReadJsonAsync(invite)).GetProperty("token").GetString();
        using var accept = await accountantClient.PostAsJsonAsync(
            "/api/employee-invitations/accept-accountant-access", new { Token = invitationToken });
        Assert.Equal(HttpStatusCode.NoContent, accept.StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await accountantClient.GetAsync($"/api/payrolls/{payrollId}")).StatusCode);
        using var accountantList = await accountantClient.GetAsync("/api/payrolls");
        var listed = Assert.Single((await ReadJsonAsync(accountantList)).EnumerateArray());
        Assert.Equal(3500m, listed.GetProperty("totalAmount").GetDecimal());

        using var accountantEdit = await accountantClient.PutAsJsonAsync(paymentUrl,
            new { Amount = 1m, Version = saved.GetProperty("version").GetGuid() });
        Assert.Equal(HttpStatusCode.NotFound, accountantEdit.StatusCode);
        using var ownerEditAfterClose = await ownerClient.PutAsJsonAsync(paymentUrl,
            new { Amount = 1m, Version = (await ReadJsonAsync(close)).GetProperty("version").GetGuid() });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, ownerEditAfterClose.StatusCode);

        using var export = await accountantClient.PostAsync($"/api/payrolls/{payrollId}/export", null);
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        Assert.StartsWith("text/csv", export.Content.Headers.ContentType?.ToString());
        var bytes = await export.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.AsSpan().StartsWith(Encoding.UTF8.GetPreamble()));
        Assert.Equal("NOME;ID_DOMINIO;SALARIO\r\nJoão da Silva;0000012345;3500,00\r\n",
            Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3));

        using var repeated = await accountantClient.PostAsync($"/api/payrolls/{payrollId}/export", null);
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        Assert.Equal(2, await db.PayrollExportEvents.CountAsync(e => e.PayrollId == payrollId));
        Assert.Equal(3500m, await db.PayrollItems.Where(i => i.PayrollId == payrollId)
            .Select(i => i.PaymentAmount).SingleAsync());

        using var secondInvite = await ownerClient.PostAsJsonAsync(
            $"/api/branches/{branchA.Id}/employee-invitations",
            new { Role = "Accountant", ExpiresInHours = 72 });
        Assert.Equal(HttpStatusCode.Created, secondInvite.StatusCode);
        var secondToken = (await ReadJsonAsync(secondInvite)).GetProperty("token").GetString();
        var newCpf = Cpf(100000005).Value;
        using var anonymousClient = factory.CreateClient();
        using var register = await anonymousClient.PostAsJsonAsync("/api/employee-invitations/accept",
            new
            {
                Token = secondToken, Name = "Nova Contadora", Cpf = newCpf,
                Password = "StrongPassword1!", StartWorkTime = TimeOnly.MinValue,
                EndWorkTime = TimeOnly.MinValue, IntervalStart = TimeOnly.MinValue,
                IntervalEnd = TimeOnly.MinValue
            });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var newAccountant = await db.Employees.AsNoTracking().SingleAsync(e => e.CPF.Value == newCpf);
        Assert.Equal(EmployeeRole.Accountant, newAccountant.Role);
        using var newAccountantClient = Client(factory, newAccountant, branchA.Id, ownerA.Id);
        Assert.Equal(HttpStatusCode.OK,
            (await newAccountantClient.GetAsync($"/api/payrolls/{payrollId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await newAccountantClient.GetAsync($"/api/branches/{branchA.Id}/employees")).StatusCode);
        using var revoke = await ownerClient.DeleteAsync(
            $"/api/payrolls/accountant-access/{branchA.Id}/{accountant.Id}");
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await accountantClient.GetAsync($"/api/payrolls/{payrollId}")).StatusCode);

        var missedStart = new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);
        var missed = new TimeKeeping(branchA.Id, worker.Id, new Geolocation(-15.6, -56.1),
            new DateOnly(2026, 10, 3), "UTC", missedStart);
        missed.ClockIn(missedStart);
        db.TimeKeepings.Add(missed);
        await db.SaveChangesAsync();
        var timeKeepingRepository = new TimeKeepingRepository(db);
        Assert.Equal(missed.Id, (await timeKeepingRepository.GetActiveByEmployeeAsync(worker.Id))?.Id);
        var correction = new TimeKeepingAdjustment(missed.Id, branchA.Id, worker.Id,
            worker.Id, TimeKeepingType.ClockOut, null, missedStart.AddHours(8),
            "Saída esquecida", missedStart.AddHours(9));
        correction.Approve(ownerA.Id, missedStart.AddHours(10));
        db.TimeKeepingAdjustments.Add(correction);
        await db.SaveChangesAsync();
        Assert.Null(await timeKeepingRepository.GetActiveByEmployeeAsync(worker.Id));
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    private static HttpClient Client(PayrollFactory factory, Employee employee, Guid branchId, Guid tenantId)
    {
        var client = factory.CreateClient();
        var token = new TokenService().GenerateToken(employee.Id, branchId, tenantId,
            employee.Role.ToString(), Secret, Issuer, Audience, 60, employee.SessionVersion);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static Branch NewBranch(string name, Document document, Guid ownerId)
        => new(name, document, new Geolocation(-15.6, -56.1),
            new Address("Rua Teste", 1, "Cuiabá", "MT", "78000000"), "UTC",
            new TimeOnly(8, 0), new TimeOnly(17, 0), ownerId,
            email: "test@example.test", isHeadquarters: true);

    private static Document Cpf(long nineDigits)
    {
        var first = nineDigits.ToString("D9");
        var ten = Digit(first, 10);
        return new Document(first + ten + Digit(first + ten, 11));
    }

    private static Document Cnpj(long twelveDigits)
    {
        var first = twelveDigits.ToString("D12");
        var firstDigit = CnpjDigit(first, 5);
        return new Document(first + firstDigit + CnpjDigit(first + firstDigit, 6));
    }

    private static int Digit(string digits, int weight)
    {
        var sum = digits.Select((c, i) => (c - '0') * (weight - i)).Sum();
        var rest = sum % 11;
        return rest < 2 ? 0 : 11 - rest;
    }

    private static int CnpjDigit(string digits, int initialWeight)
    {
        var weight = initialWeight;
        var sum = 0;
        foreach (var c in digits)
        {
            sum += (c - '0') * weight;
            weight = weight == 2 ? 9 : weight - 1;
        }
        var rest = sum % 11;
        return rest < 2 ? 0 : 11 - rest;
    }

    private sealed class PayrollFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("MassTransit:Disabled", "true");
            builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DottInDb"] = connectionString,
                    ["JwtSettings:SecretKey"] = Secret,
                    ["JwtSettings:Issuer"] = Issuer,
                    ["JwtSettings:Audience"] = Audience,
                    ["JwtSettings:ExpirationMinutes"] = "60",
                    ["Database:ApplyMigrationsOnStartup"] = "false",
                    ["MassTransit:Disabled"] = "true"
                }));
        }
    }
}
