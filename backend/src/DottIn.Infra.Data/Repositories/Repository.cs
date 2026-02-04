using DottIn.Domain.Core.Data;
using DottIn.Domain.Core.Models;
using DottIn.Infra.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Infra.Data.Repositories
{
    public class Repository<TEntity, TId> : IRepository<TEntity, TId>
        where TEntity : class, IAggregateRoot
        where TId : notnull
    {
        protected DottInContext _context;

        protected DbSet<TEntity> _dbSet;

        public Repository(DottInContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<TEntity>();
        }

        public async Task AddAsync(TEntity entity, CancellationToken token = default)
        {
            await _dbSet.AddAsync(entity, token);
        }

        public Task DeleteAsync(TEntity entity)
        {
            _dbSet.Remove(entity);

            return Task.CompletedTask;
        }

        public async Task<TEntity?> GetByIdAsync(TId id, CancellationToken token = default)
        {
            return await _dbSet.FindAsync(id, token).AsTask();
        }

        public async Task UpdateAsync(TEntity entity)
        {
            _dbSet.Update(entity);

            await Task.CompletedTask;
        }
    }
}
