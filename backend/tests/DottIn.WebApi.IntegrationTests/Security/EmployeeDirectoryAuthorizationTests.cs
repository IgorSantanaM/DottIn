using System.Reflection;
using System.Security.Claims;
using DottIn.Infra.Data.Contexts;
using DottIn.Presentation.WebApi.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace DottIn.WebApi.IntegrationTests.Security;

public sealed class EmployeeDirectoryAuthorizationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("/active")]
    [InlineData("/paged")]
    [InlineData("/paged/")]
    [InlineData("/PAGED")]
    [InlineData("/cpf/11122233396")]
    public async Task EmployeeCannotReadDirectoryEvenInTheirOwnBranch(string suffix)
    {
        var branchId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, employeeId.ToString()),
            new Claim(ClaimTypes.Role, "Employee"),
            new Claim("branchId", branchId.ToString()),
            new Claim("tenantId", Guid.NewGuid().ToString())], "test"));
        http.Request.Method = "GET";
        http.Request.Path = $"/api/branches/{branchId}/employees{suffix}";
        http.Request.RouteValues["branchId"] = branchId;
        var current = new CurrentUserContext(new HttpContextAccessor { HttpContext = http });
        // A forbidden request must be rejected before touching the database or handler.
        await using var db = new DottInContext(new DbContextOptionsBuilder<DottInContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused").Options);
        var filter = new TenantAuthorizationFilter(new TenantAccessService(db, current), current);
        var called = false;
        var result = await filter.InvokeAsync(new DefaultEndpointFilterInvocationContext(http), _ =>
        {
            called = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });
        Assert.IsType<ForbidHttpResult>(result);
        Assert.False(called);
    }

    [Fact]
    public void OwnProfileReadIsNotTreatedAsDirectoryRead()
    {
        var classify = typeof(TenantAuthorizationFilter).GetMethod("IsEmployeeDirectoryPath", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.False((bool)classify.Invoke(null, [$"/api/branches/{Guid.NewGuid()}/employees/{Guid.NewGuid()}"])!);
    }
}
