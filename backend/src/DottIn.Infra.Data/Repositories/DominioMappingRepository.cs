using DottIn.Domain.Exports;
using DottIn.Infra.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Infra.Data.Repositories;

public class DominioMappingRepository(DottInContext context) : IDominioMappingRepository
{
    public async Task<IEnumerable<DominioEmployeeMapping>> GetByBranchAsync(Guid branchId, CancellationToken token = default)
    {
        return await context.DominioEmployeeMappings
            .AsNoTracking()
            .Where(d => d.BranchId == branchId)
            .ToListAsync(token);
    }

    public async Task<DominioEmployeeMapping?> GetByEmployeeAsync(Guid employeeId, CancellationToken token = default)
    {
        return await context.DominioEmployeeMappings
            .FirstOrDefaultAsync(d => d.EmployeeId == employeeId, token);
    }

    public async Task AddAsync(DominioEmployeeMapping mapping, CancellationToken token = default)
    {
        await context.DominioEmployeeMappings.AddAsync(mapping, token);
    }

    public Task UpdateAsync(DominioEmployeeMapping mapping)
    {
        context.DominioEmployeeMappings.Update(mapping);
        return Task.CompletedTask;
    }
}
