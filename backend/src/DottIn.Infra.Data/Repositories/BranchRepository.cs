using DottIn.Domain.Branches;
using DottIn.Infra.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Infra.Data.Repositories
{
    public class BranchRepository(DottInContext context) : Repository<Branch, Guid>(context), IBranchRepository
    {
        // Transaction-scoped locks serialize quota checks across API instances, not just within one process.
        public async Task LockOwnerAsync(Guid ownerId, CancellationToken token = default)
            => await context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({"branch-owner:" + ownerId.ToString()}, 0))", token);

        public async Task LockDocumentAsync(string document, CancellationToken token = default)
            => await context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({"branch-document:" + document}, 0))", token);

        public Task ReloadAsync(Branch branch, CancellationToken token = default)
            => context.Entry(branch).ReloadAsync(token);

        public async Task<IEnumerable<Branch>> GetActiveBranchesAsync(CancellationToken token = default)
            => await context.Branches
                .AsNoTracking()
                .Where(b => b.IsActive)
                .ToListAsync(token);

        public async Task<Branch?> GetByDocumentAsync(string document, CancellationToken token = default)
        {
            var sanitizedDocument = new string(document.Where(char.IsDigit).ToArray());
            return await context.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Document.Value == sanitizedDocument, token);
        }

        public async Task<Branch?> GetByCodeAsync(string companyCode, CancellationToken token = default)
        {
            var normalizedCode = companyCode.Trim().ToLowerInvariant().Replace(" ", "-");
            return await context.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.CompanyCode.ToLower() == normalizedCode, token);
        }

        public async Task<IEnumerable<Branch>> GetByOwnerIdAsync(Guid ownerId, CancellationToken token = default)
            => await context.Branches
                .AsNoTracking()
                .Where(b => b.OwnerId == ownerId)
                .ToListAsync(token);

        public async Task<IEnumerable<Branch>?> GetHeadquartersAsync(CancellationToken token = default)
            => await context.Branches
                .AsNoTracking()
                .Where(b => b.IsHeadquarters)
                .ToListAsync(token);

        public async Task<int> CountActiveByOwnerIdAsync(Guid ownerId, CancellationToken token = default)
            => await context.Branches
                .AsNoTracking()
                .Where(b => b.OwnerId == ownerId && b.IsActive)
                .CountAsync(token);
    }
}
