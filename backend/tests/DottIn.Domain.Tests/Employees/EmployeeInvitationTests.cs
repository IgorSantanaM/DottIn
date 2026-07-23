using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;

namespace DottIn.Domain.Tests.Employees;

public class EmployeeInvitationTests
{
    [Fact]
    public void PendingInvitation_CanBeConsumedOnlyOnce()
    {
        var now = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        var invitation = new EmployeeInvitation(
            Guid.NewGuid(), Guid.NewGuid(), "HASH", EmployeeRole.Employee,
            now.AddDays(7), now, "pessoa@empresa.com.br");

        invitation.Consume(Guid.NewGuid(), now.AddMinutes(5));

        Assert.Equal(InvitationStatus.Consumed, invitation.StatusAt(now.AddMinutes(6)));
        Assert.Throws<DomainException>(() => invitation.Consume(Guid.NewGuid(), now.AddMinutes(7)));
    }

    [Fact]
    public void ExpiredOrRevokedInvitation_CannotBeConsumed()
    {
        var now = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        var expired = new EmployeeInvitation(
            Guid.NewGuid(), Guid.NewGuid(), "HASH1", EmployeeRole.Employee,
            now.AddMinutes(-1), now.AddDays(-1));
        var revoked = new EmployeeInvitation(
            Guid.NewGuid(), Guid.NewGuid(), "HASH2", EmployeeRole.Manager,
            now.AddDays(7), now);
        revoked.Revoke(now.AddMinutes(1));

        Assert.Equal(InvitationStatus.Expired, expired.StatusAt(now));
        Assert.Throws<DomainException>(() => expired.Consume(Guid.NewGuid(), now));
        Assert.Throws<DomainException>(() => revoked.Consume(Guid.NewGuid(), now.AddMinutes(2)));
    }

    [Fact]
    public void Renew_InvalidatesPreviousHashAndRestoresPendingState()
    {
        var now = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        var invitation = new EmployeeInvitation(
            Guid.NewGuid(), Guid.NewGuid(), "OLD", EmployeeRole.Employee,
            now.AddDays(7), now);

        invitation.Renew("NEW", now.AddDays(8), now.AddDays(1));

        Assert.Equal("NEW", invitation.TokenHash);
        Assert.Equal(InvitationStatus.Pending, invitation.StatusAt(now.AddDays(1)));
    }
}
