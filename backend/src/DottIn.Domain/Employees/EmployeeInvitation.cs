using DottIn.Domain.Branches;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Core.Models;

namespace DottIn.Domain.Employees;

public sealed class EmployeeInvitation : Entity<Guid>, IAggregateRoot
{
    public Guid BranchId { get; private set; }
    public Guid InvitedByEmployeeId { get; private set; }
    public string TokenHash { get; private set; } = "";
    public string? Email { get; private set; }
    public EmployeeRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime? ConsumedAt { get; private set; }
    public Guid? ConsumedByEmployeeId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    private EmployeeInvitation() { }

    public EmployeeInvitation(
        Guid branchId,
        Guid invitedByEmployeeId,
        string tokenHash,
        EmployeeRole role,
        DateTime expiresAtUtc,
        DateTime createdAtUtc,
        string? email = null)
    {
        if (branchId == Guid.Empty)
            throw new DomainException("A filial do convite é obrigatória.");
        if (invitedByEmployeeId == Guid.Empty)
            throw new DomainException("O responsável pelo convite é obrigatório.");
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("Token de convite inválido.");
        if (role == EmployeeRole.Owner)
            throw new DomainException("Não é permitido convidar outro proprietário.");

        BranchTime.NormalizeUtc(createdAtUtc);
        BranchTime.NormalizeUtc(expiresAtUtc);
        if (expiresAtUtc <= createdAtUtc)
            throw new DomainException("A validade do convite deve ser futura.");

        Id = Guid.NewGuid();
        BranchId = branchId;
        InvitedByEmployeeId = invitedByEmployeeId;
        TokenHash = tokenHash;
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        Role = role;
        CreatedAt = createdAtUtc;
        ExpiresAt = expiresAtUtc;
        ConcurrencyToken = Guid.NewGuid();
    }

    public InvitationStatus StatusAt(DateTime nowUtc)
    {
        BranchTime.NormalizeUtc(nowUtc);
        if (ConsumedAt.HasValue) return InvitationStatus.Consumed;
        if (RevokedAt.HasValue) return InvitationStatus.Revoked;
        return nowUtc >= ExpiresAt ? InvitationStatus.Expired : InvitationStatus.Pending;
    }

    public void Consume(Guid employeeId, DateTime consumedAtUtc)
    {
        if (employeeId == Guid.Empty)
            throw new DomainException("Funcionário inválido.");
        if (StatusAt(consumedAtUtc) != InvitationStatus.Pending)
            throw new DomainException("Este convite não está mais disponível.");

        ConsumedByEmployeeId = employeeId;
        ConsumedAt = consumedAtUtc;
        UpdatedAt = consumedAtUtc;
        ConcurrencyToken = Guid.NewGuid();
    }

    public void Revoke(DateTime revokedAtUtc)
    {
        if (StatusAt(revokedAtUtc) == InvitationStatus.Consumed)
            throw new DomainException("Um convite já utilizado não pode ser revogado.");

        RevokedAt = revokedAtUtc;
        UpdatedAt = revokedAtUtc;
        ConcurrencyToken = Guid.NewGuid();
    }

    public void Renew(string tokenHash, DateTime expiresAtUtc, DateTime renewedAtUtc)
    {
        if (ConsumedAt.HasValue)
            throw new DomainException("Um convite já utilizado não pode ser renovado.");
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("Token de convite inválido.");

        BranchTime.NormalizeUtc(renewedAtUtc);
        BranchTime.NormalizeUtc(expiresAtUtc);
        if (expiresAtUtc <= renewedAtUtc)
            throw new DomainException("A validade do convite deve ser futura.");

        TokenHash = tokenHash;
        ExpiresAt = expiresAtUtc;
        RevokedAt = null;
        UpdatedAt = renewedAtUtc;
        ConcurrencyToken = Guid.NewGuid();
    }
}
