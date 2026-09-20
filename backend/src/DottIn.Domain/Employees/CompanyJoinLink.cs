using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Core.Models;

namespace DottIn.Domain.Employees;

/// <summary>A revocable, reusable invitation link for a company branch.</summary>
public sealed class CompanyJoinLink : Entity<Guid>, IAggregateRoot
{
    public Guid BranchId { get; private set; }
    public Guid CreatedByEmployeeId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    private CompanyJoinLink() { }

    public CompanyJoinLink(Guid branchId, Guid createdByEmployeeId, DateTime expiresAtUtc)
    {
        if (branchId == Guid.Empty || createdByEmployeeId == Guid.Empty)
            throw new DomainException("A empresa e o responsável pelo link são obrigatórios.");
        if (expiresAtUtc <= DateTime.UtcNow)
            throw new DomainException("A validade do link deve ser futura.");

        Id = Guid.NewGuid();
        BranchId = branchId;
        CreatedByEmployeeId = createdByEmployeeId;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc);
        ConcurrencyToken = Guid.NewGuid();
    }

    public bool IsActiveAt(DateTime nowUtc) => RevokedAt is null && nowUtc < ExpiresAt;

    public void Revoke(DateTime revokedAtUtc)
    {
        if (RevokedAt is not null) return;
        RevokedAt = DateTime.SpecifyKind(revokedAtUtc, DateTimeKind.Utc);
        ConcurrencyToken = Guid.NewGuid();
    }

    public void Renew(DateTime expiresAtUtc)
    {
        if (expiresAtUtc <= DateTime.UtcNow)
            throw new DomainException("A validade do link deve ser futura.");
        ExpiresAt = DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc);
        RevokedAt = null;
        ConcurrencyToken = Guid.NewGuid();
    }
}
