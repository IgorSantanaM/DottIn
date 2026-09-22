using DottIn.Domain.Core.Data;

namespace DottIn.Domain.Employees
{
    public interface IEmployeeRepository : IRepository<Employee, Guid>
    {
        Task<bool> AddEmployeeImageAsync(Guid employeeId, string imageUrl, CancellationToken cancellationToken = default);
        Task<bool> UpdateEmployeeImageAsync(Guid employeeId, string imageUrl, CancellationToken cancellationToken = default);
        Task<Employee?> GetByCPFAsync(Guid branchId, string cpf, CancellationToken token = default);
        Task<Employee?> GetByCPFAsync(string cpf, CancellationToken token = default);
        Task<Employee?> GetByTenantAndCPFAsync(Guid tenantId, string cpf, CancellationToken token = default);
        Task<IEnumerable<Employee>> GetByBranchIdAsync(Guid branchId, CancellationToken token = default);
        Task<IReadOnlyDictionary<Guid, string>> GetNamesByIdsAsync(Guid branchId, IReadOnlyCollection<Guid> employeeIds, CancellationToken token = default);
        Task<IEnumerable<Employee>> GetActiveEmployeesAsync(Guid branchId, CancellationToken token = default);
        Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedByBranchIdAsync(
            Guid branchId,
            int pageNumber,
            int pageSize,
            string? search,
            bool? isActive,
            CancellationToken token = default);
        Task<int> CountActiveByOwnerIdAsync(Guid ownerId, CancellationToken token = default);
        Task<int> CountActiveByBranchIdAsync(Guid branchId, CancellationToken token = default);
    }
}
