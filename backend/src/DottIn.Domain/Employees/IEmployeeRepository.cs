using DottIn.Domain.Core.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace DottIn.Domain.Employees
{
    public interface IEmployeeRepository : IRepository<Employee, Guid>
    {
        Task<Employee?> GetByCPFAsync(string cpf);
        Task<IEnumerable<Employee>> GetByBranchIdAsync(Guid branchId);
        Task<IEnumerable<Employee>> GetActiveEmployeesAsync(Guid branchId);
    }
}
