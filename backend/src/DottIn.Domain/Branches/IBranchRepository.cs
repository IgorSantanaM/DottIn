using DottIn.Domain.Core.Data;

namespace DottIn.Domain.Branches
{
    public interface IBranchRepository : IRepository<Branch, Guid>
    {
        Task<Branch?> GetByDocumentAsync(string document);
        Task<IEnumerable<Branch>> GetByOwnerIdAsync(string ownerId);
        Task<IEnumerable<Branch>> GetActiveBranchesAsync();
        Task<Branch?> GetHeadquartersAsync();
    }
}
