using DottIn.Domain.Core.Models;

namespace DottIn.Domain.Core.Data
{
    public interface IRepository<TEntity, in TId> 
        where TEntity : IAggregateRoot 
        where TId : notnull
    {
        Task AddAsync(TEntity entity, CancellationToken token = default);
        Task UpdateAsync(TEntity entity);
        Task DeleteAsync(TEntity entity);
        Task<TEntity?> GetByIdAsync(TId id, CancellationToken token = default);
    }
}
