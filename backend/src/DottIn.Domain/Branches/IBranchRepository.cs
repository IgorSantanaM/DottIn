using DottIn.Domain.Core.Data;

namespace DottIn.Domain.Branches
{
    public interface IBranchRepository : IRepository<Branch, Guid>
    {
        Task<Branch?> GetByDocumentAsync(string document, CancellationToken token = default);
        Task<IEnumerable<Branch>> GetByOwnerIdAsync(Guid ownerId, CancellationToken token = default);
        Task<IEnumerable<Branch>> GetActiveBranchesAsync(CancellationToken token = default);
        Task<IEnumerable<Branch>?> GetHeadquartersAsync(CancellationToken token = default);
    }
}
