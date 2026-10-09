using DottIn.Mobile.Services;

namespace DottIn.Mobile.Tests;

public sealed class AuthenticationAccessMessagesTests
{
    [Fact]
    public void InactiveEmployeeHasClearMembershipMessage()
        => Assert.Equal(AuthenticationAccessMessages.InactiveEmployee,
            AuthenticationAccessMessages.Forbidden("{\"code\":\"employee_inactive\"}"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("<html>proxy error</html>")]
    [InlineData("{\"code\":123}")]
    [InlineData("{\"code\":\"different_error\"}")]
    public void OtherOrMalformedForbiddenResponsesStayFriendly(string? content)
        => Assert.Contains("não tem acesso", AuthenticationAccessMessages.Forbidden(content));
}
