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

        public async Task<IReadOnlyDictionary<Guid, string>> GetNamesByIdsAsync(
            Guid branchId,
            IReadOnlyCollection<Guid> employeeIds,
            CancellationToken token = default)
        {
            var ids = employeeIds.Distinct().ToArray();
            if (ids.Length == 0)
                return new Dictionary<Guid, string>();

            return await context.Employees
                .AsNoTracking()
                .Where(employee => employee.BranchId == branchId && ids.Contains(employee.Id))
                .Select(employee => new { employee.Id, employee.Name })
                .ToDictionaryAsync(employee => employee.Id, employee => employee.Name, token);
        }
        public async Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedByBranchIdAsync(
            Guid branchId,
            int pageNumber,
            int pageSize,
            string? search,
            bool? isActive,
            CancellationToken token = default)
        {
            var query = context.Employees
                .AsNoTracking()
                .Where(e => e.BranchId == branchId);

            if (isActive.HasValue)
                query = query.Where(e => e.IsActive == isActive.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                var digits = new string(term.Where(char.IsDigit).ToArray());
                query = query.Where(e => EF.Functions.ILike(e.Name, $"%{term}%") ||
                    (digits.Length > 0 && e.CPF.Value.Contains(digits)));
            }

            var totalCount = await query.CountAsync(token);
            var items = await query
                .OrderBy(e => e.Name)
                .ThenBy(e => e.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(token);

            return (items, totalCount);
        }
        public async Task<Employee?> GetByCPFAsync(string cpf, CancellationToken token = default)
        {
            var sanitizedCpf = new string(cpf.Where(char.IsDigit).ToArray());
            return await context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.CPF.Value == sanitizedCpf, token);
        }

        public async Task<Employee?> GetByCPFAsync(Guid branchId, string cpf, CancellationToken token = default)
        {
            var sanitizedCpf = new string(cpf.Where(char.IsDigit).ToArray());
            return await context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.BranchId == branchId && e.CPF.Value == sanitizedCpf, token);
        }

        public async Task<Employee?> GetByTenantAndCPFAsync(Guid tenantId, string cpf, CancellationToken token = default)
        {
            var sanitizedCpf = new string(cpf.Where(char.IsDigit).ToArray());
            var branchIds = context.Branches
                .Where(b => b.OwnerId == tenantId || b.Id == tenantId)
                .Select(b => b.Id);

            return await context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => branchIds.Contains(e.BranchId) && e.CPF.Value == sanitizedCpf, token);
        }

        public Task<int> CountActiveByBranchIdAsync(Guid branchId, CancellationToken token = default)
            => context.Employees
                .AsNoTracking()
                .CountAsync(e => e.BranchId == branchId && e.IsActive && e.Role != EmployeeRole.Owner, token);
        public async Task<int> CountActiveByOwnerIdAsync(Guid ownerId, CancellationToken token = default)
        {
            var branchIds = await context.Branches
                .AsNoTracking()
                .Where(b => b.OwnerId == ownerId && b.IsActive)
                .Select(b => b.Id)
                .ToListAsync(token);

            return await context.Employees
                .AsNoTracking()
                .Where(e => branchIds.Contains(e.BranchId) && e.IsActive && e.Role != EmployeeRole.Owner)
                .CountAsync(token);
        }

        public async Task<bool> AddEmployeeImageAsync(Guid employeeId, string imageUrl, CancellationToken cancellationToken = default)
        {
            var employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);

            if (employee is null)
                return false;

            employee.AddImage(imageUrl);

            await UpdateAsync(employee);

            return true;
        }

        public async Task<bool> UpdateEmployeeImageAsync(Guid employeeId, string imageUrl, CancellationToken cancellationToken = default)
        {
            var employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);

            if (employee is null)
                return false;

            employee.RemoveImage();

            employee.AddImage(imageUrl);

            await UpdateAsync(employee);

            return true;
        }
    }
}
