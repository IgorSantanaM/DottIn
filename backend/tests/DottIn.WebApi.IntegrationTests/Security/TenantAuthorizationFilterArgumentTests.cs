using System.Reflection;
using System.Security.Claims;
using DottIn.Presentation.WebApi.Security;
using Microsoft.AspNetCore.Http;

namespace DottIn.WebApi.IntegrationTests.Security;

public sealed class TenantAuthorizationFilterArgumentTests
{
    [Fact]
    public void InjectedCurrentUserIsNotInterpretedAsRequestTarget()
    {
        var ownerId = Guid.NewGuid();
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, ownerId.ToString()),
                 new Claim("tenantId", ownerId.ToString()),
                 new Claim(ClaimTypes.Role, "Owner")], "test"))
        };
        var currentUser = new CurrentUserContext(new HttpContextAccessor { HttpContext = http });
        var arguments = new List<object?> { currentUser, new { OwnerId = ownerId } };
        var method = typeof(TenantAuthorizationFilter).GetMethod(
            "ReadArgumentGuid", BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.Null(method.Invoke(null, [arguments, "EmployeeId"]));
    }
}
