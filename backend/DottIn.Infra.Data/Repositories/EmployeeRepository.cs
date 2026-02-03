using DottIn.Domain.Employees;
using DottIn.Infra.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DottIn.Infra.Data.Repositories
{
    public class EmployeeRepository(DottInContext context) : Repository<Employee, Guid>(context), IEmployeeRepository
    {
        public async Task<IEnumerable<Employee>> GetActiveEmployeesAsync(Guid branchId, CancellationToken token = default)
            => await context.Employees
                .AsNoTracking()
                .Where(e => e.IsActive).ToListAsync(token);

        public async Task<IEnumerable<Employee>> GetByBranchIdAsync(Guid branchId, CancellationToken token = default)
            => await context.Employees
                .AsNoTracking()
                .Where(e => e.BranchId ==  branchId).ToListAsync(token);

        public async Task<Employee?> GetByCPFAsync(string cpf, CancellationToken token = default)
            => await context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => EF.Functions.ILike(e.CPF.Value, $"%{cpf}%"), token);
    }
}
