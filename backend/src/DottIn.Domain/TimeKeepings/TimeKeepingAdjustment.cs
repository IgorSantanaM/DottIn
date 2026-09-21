using DottIn.Domain.Branches;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Core.Models;

namespace DottIn.Domain.TimeKeepings;

public sealed class TimeKeepingAdjustment : Entity<Guid>, IAggregateRoot
{
    public Guid TimeKeepingId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid RequestedByEmployeeId { get; private set; }
    public Guid? ReviewedByEmployeeId { get; private set; }
    public TimeKeepingType EntryType { get; private set; }
    public DateTime? OriginalTimestamp { get; private set; }
    public DateTime ProposedTimestamp { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string? ReviewNote { get; private set; }
    public TimeKeepingAdjustmentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid ConcurrencyToken { get; private set; } = Guid.NewGuid();

    private TimeKeepingAdjustment() { }

    public TimeKeepingAdjustment(
        Guid timeKeepingId,
        Guid branchId,
        Guid employeeId,
        Guid requestedByEmployeeId,
        TimeKeepingType entryType,
        DateTime? originalTimestamp,
        DateTime proposedTimestamp,
        string reason,
        DateTime createdAtUtc)
    {
        if (timeKeepingId == Guid.Empty || branchId == Guid.Empty || employeeId == Guid.Empty || requestedByEmployeeId == Guid.Empty)
            throw new DomainException("Os identificadores da solicitação de correção são obrigatórios.");

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5 || reason.Trim().Length > 500)
            throw new DomainException("A justificativa deve conter entre 5 e 500 caracteres.");

        BranchTime.NormalizeUtc(proposedTimestamp);
        BranchTime.NormalizeUtc(createdAtUtc);
        if (originalTimestamp.HasValue)
            BranchTime.NormalizeUtc(originalTimestamp.Value);

        Id = Guid.NewGuid();
        TimeKeepingId = timeKeepingId;
        BranchId = branchId;
        EmployeeId = employeeId;
        RequestedByEmployeeId = requestedByEmployeeId;
        EntryType = entryType;
        OriginalTimestamp = originalTimestamp;
        ProposedTimestamp = proposedTimestamp;
        Reason = reason.Trim();
        Status = TimeKeepingAdjustmentStatus.Pending;
        CreatedAt = createdAtUtc;
    }

    public void Approve(Guid reviewerEmployeeId, DateTime reviewedAtUtc, string? reviewNote = null)
        => Review(reviewerEmployeeId, reviewedAtUtc, TimeKeepingAdjustmentStatus.Approved, reviewNote);

    public void Reject(Guid reviewerEmployeeId, DateTime reviewedAtUtc, string? reviewNote = null)
        => Review(reviewerEmployeeId, reviewedAtUtc, TimeKeepingAdjustmentStatus.Rejected, reviewNote);

    private void Review(
        Guid reviewerEmployeeId,
        DateTime reviewedAtUtc,
        TimeKeepingAdjustmentStatus status,
        string? reviewNote)
    {
        if (Status != TimeKeepingAdjustmentStatus.Pending)
            throw new DomainException("Esta solicitação já foi analisada.");
        if (reviewerEmployeeId == Guid.Empty)
            throw new DomainException("O responsável pela análise é obrigatório.");
        if (reviewerEmployeeId == RequestedByEmployeeId)
            throw new DomainException("O solicitante não pode aprovar a própria correção.");
        if (reviewNote?.Length > 500)
            throw new DomainException("A observação da análise deve ter no máximo 500 caracteres.");

        BranchTime.NormalizeUtc(reviewedAtUtc);
        ReviewedByEmployeeId = reviewerEmployeeId;
        ReviewedAt = reviewedAtUtc;
        ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? null : reviewNote.Trim();
        Status = status;
        ConcurrencyToken = Guid.NewGuid();
    }
}
