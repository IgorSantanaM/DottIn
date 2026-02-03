using DottIn.Domain.Branches;
using DottIn.Infra.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Infra.Data.Repositories
{
    public class BranchRepository(DottInContext context) : Repository<Branch, Guid>(context), IBranchRepository
    {
        public async Task<IEnumerable<Branch>> GetActiveBranchesAsync(CancellationToken token = default)
            => await context.Branches
                .AsNoTracking()
                .Where(b => b.IsActive).ToListAsync(token);

        public async Task<Branch?> GetByDocumentAsync(string document, CancellationToken token = default)
            => await context.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(b => EF.Functions.ILike(b.Document.Value, $"%{document}%"), token);

        public async Task<IEnumerable<Branch>> GetByOwnerIdAsync(string ownerId, CancellationToken token = default)
            => await context.Branches
                .AsNoTracking()
                .Where(b => b.OwnerId == ownerId).ToListAsync(token);

        public async Task<IEnumerable<Branch>?> GetHeadquartersAsync(CancellationToken token = default)
        => await context.Branches
                .AsNoTracking()
                .Where(b => b.IsHeadquarters).ToListAsync(token); // TODO: Get depending on the branch / HeadquarterId prop or something like that
    }
}
