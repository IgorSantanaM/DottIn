using DottIn.Domain.Employees;

namespace DottIn.Domain.Tests.Employees;

public sealed class CompanyJoinLinkTests
{
    [Fact]
    public void Link_IsReusableUntilItExpiresOrIsRevoked()
    {
        var link = new CompanyJoinLink(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1));

        Assert.True(link.IsActiveAt(DateTime.UtcNow));
        Assert.True(link.IsActiveAt(DateTime.UtcNow.AddHours(1)));

        link.Revoke(DateTime.UtcNow);
        Assert.False(link.IsActiveAt(DateTime.UtcNow.AddMinutes(1)));
    }
}
