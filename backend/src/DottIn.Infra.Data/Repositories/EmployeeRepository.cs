using DottIn.Domain.Employees;
using DottIn.Infra.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Infra.Data.Repositories
{
    public class EmployeeRepository(DottInContext context) : Repository<Employee, Guid>(context), IEmployeeRepository
    {
        public async Task<IEnumerable<Employee>> GetActiveEmployeesAsync(Guid branchId, CancellationToken token = default)
            => await context.Employees
                .AsNoTracking()
                .Where(e => e.BranchId == branchId && e.IsActive)
                .ToListAsync(token);

        public async Task<IEnumerable<Employee>> GetByBranchIdAsync(Guid branchId, CancellationToken token = default)
            => await context.Employees
                .AsNoTracking()
                .Where(e => e.BranchId == branchId)
                .ToListAsync(token);

        public async Task<Employee?> GetByCPFAsync(string cpf, CancellationToken token = default)
        {
            var sanitizedCpf = new string(cpf.Where(char.IsDigit).ToArray());
            return await context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.CPF.Value == sanitizedCpf, token);
        }
    }
}
