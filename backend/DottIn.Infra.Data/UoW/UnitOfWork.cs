using DottIn.Domain.Core.Data;
using DottIn.Infra.Data.Contexts;

namespace DottIn.Infra.Data.UoW
{
    public class UnitOfWork(DottInContext context) : IUnitOfWork, IDisposable
    {
        private bool _disposed;
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => await context.SaveChangesAsync();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                context.Dispose();
            }
            _disposed = true;
        }
    }
}
