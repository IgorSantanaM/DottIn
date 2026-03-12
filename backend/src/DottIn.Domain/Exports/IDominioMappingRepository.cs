namespace DottIn.Domain.Exports;

public interface IDominioMappingRepository
{
    Task<IEnumerable<DominioEmployeeMapping>> GetByBranchAsync(Guid branchId, CancellationToken token = default);
    Task<DominioEmployeeMapping?> GetByEmployeeAsync(Guid employeeId, CancellationToken token = default);
    Task AddAsync(DominioEmployeeMapping mapping, CancellationToken token = default);
    Task UpdateAsync(DominioEmployeeMapping mapping);
}
