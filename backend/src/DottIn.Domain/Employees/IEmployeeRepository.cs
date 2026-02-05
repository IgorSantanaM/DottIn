using DottIn.Domain.Core.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace DottIn.Domain.Employees
{
    public interface IEmployeeRepository : IRepository<Employee, Guid>
    {
        Task<bool> AddEmployeeImageAsync(Guid employeeId, string imageUrl, CancellationToken cancellationToken = default);
        Task<bool> UpdateEmployeeImageAsync(Guid employeeId, string imageUrl, CancellationToken cancellationToken = default);
        Task<Employee?> GetByCPFAsync(string cpf, CancellationToken token = default);
        Task<IEnumerable<Employee>> GetByBranchIdAsync(Guid branchId, CancellationToken token = default);
        Task<IEnumerable<Employee>> GetActiveEmployeesAsync(Guid branchId, CancellationToken token = default);
    }
}
