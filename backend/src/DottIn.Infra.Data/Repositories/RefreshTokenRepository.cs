using DottIn.Domain.Auth;
using DottIn.Infra.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Infra.Data.Repositories
{
    public class RefreshTokenRepository(DottInContext context) : IRefreshTokenRepository
    {
        public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
            => await context.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == token, cancellationToken);

        public async Task<IEnumerable<RefreshToken>> GetActiveByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
            => await context.RefreshTokens
                .Where(r => r.EmployeeId == employeeId && r.RevokedAt == null)
                .ToListAsync(cancellationToken);

        public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
            => await context.RefreshTokens.AddAsync(refreshToken, cancellationToken);

        public async Task UpdateAsync(RefreshToken refreshToken)
        {
            context.RefreshTokens.Update(refreshToken);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(RefreshToken refreshToken)
        {
            context.RefreshTokens.Remove(refreshToken);
            await Task.CompletedTask;
        }

        public async Task DeleteAllByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            await context.RefreshTokens
                .Where(r => r.EmployeeId == employeeId)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
