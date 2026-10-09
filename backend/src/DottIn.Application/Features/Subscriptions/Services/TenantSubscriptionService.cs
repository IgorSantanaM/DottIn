using DottIn.Application.Features.Subscriptions.DTOs;
using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using DottIn.Domain.Subscriptions;

namespace DottIn.Application.Features.Subscriptions.Services
{
    public class TenantSubscriptionService : ITenantSubscriptionService
    {
        private readonly ITenantSubscriptionRepository _subscriptionRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IBranchRepository _branchRepository;

        public TenantSubscriptionService(
            ITenantSubscriptionRepository subscriptionRepository,
            IEmployeeRepository employeeRepository,
            IBranchRepository branchRepository)
        {
            _subscriptionRepository = subscriptionRepository;
            _employeeRepository = employeeRepository;
            _branchRepository = branchRepository;
        }

        public async Task<TenantSubscriptionDto?> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
        {
            var subscription = await GetCompanySubscriptionAsync(ownerId, cancellationToken);
            if (subscription?.Plan == null)
                return null;

            return await MapToDto(subscription, cancellationToken);
        }

        public async Task<TenantSubscriptionDto?> GetByHeadquartersIdAsync(Guid headquartersId, CancellationToken cancellationToken = default)
        {
            var subscription = await _subscriptionRepository.GetByHeadquartersIdAsync(headquartersId, cancellationToken);
            if (subscription?.Plan == null)
                return null;

            return await MapToDto(subscription, cancellationToken);
        }

        public async Task<bool> CanAddEmployeeAsync(Guid ownerId, CancellationToken cancellationToken = default)
        {
            var subscription = await _subscriptionRepository.GetByOwnerIdAsync(ownerId, cancellationToken);
            if (subscription?.Plan == null)
                return false;

            if (!subscription.IsActiveOrTrialing)
                return false;

            if (subscription.Plan.HasUnlimitedEmployees)
                return true;

            var currentCount = await _employeeRepository.CountActiveByOwnerIdAsync(ownerId, cancellationToken);
            return subscription.Plan.CanAddEmployee(currentCount);
        }

        public async Task<bool> CanAddBranchAsync(Guid ownerId, CancellationToken cancellationToken = default)
        {
            var subscription = await GetCompanySubscriptionAsync(ownerId, cancellationToken);
            if (subscription?.Plan == null)
                return false;

            if (!subscription.IsActiveOrTrialing)
                return false;

            if (subscription.Plan.HasUnlimitedBranches)
                return true;

            var currentCount = await _branchRepository.CountActiveByOwnerIdAsync(ownerId, cancellationToken);
            return subscription.Plan.CanAddBranch(currentCount);
        }

        public async Task<int> GetEmployeeCountAsync(Guid ownerId, CancellationToken cancellationToken = default)
        {
            return await _employeeRepository.CountActiveByOwnerIdAsync(ownerId, cancellationToken);
        }

        public async Task<int> GetBranchCountAsync(Guid ownerId, CancellationToken cancellationToken = default)
        {
            return await _branchRepository.CountActiveByOwnerIdAsync(ownerId, cancellationToken);
        }

        private async Task<TenantSubscription?> GetCompanySubscriptionAsync(Guid companyOwnerId, CancellationToken token)
        {
            // Billing ownership may differ from headquarters ownership. Resolve the plan through
            // the persisted headquarters, never through a branch or owner ID supplied by the browser.
            var headquarters = (await _branchRepository.GetByOwnerIdAsync(companyOwnerId, token)).FirstOrDefault(b => b.IsHeadquarters);
            return headquarters is not null
                ? await _subscriptionRepository.GetByHeadquartersIdAsync(headquarters.Id, token)
                : await _subscriptionRepository.GetByOwnerIdAsync(companyOwnerId, token);
        }

        private async Task<TenantSubscriptionDto> MapToDto(TenantSubscription subscription, CancellationToken cancellationToken)
        {
            var headquarters = await _branchRepository.GetByIdAsync(subscription.HeadquartersId, cancellationToken);
            var companyOwnerId = headquarters?.OwnerId ?? subscription.OwnerId;
            var employeeCount = await _employeeRepository.CountActiveByOwnerIdAsync(companyOwnerId, cancellationToken);
            var branchCount = await _branchRepository.CountActiveByOwnerIdAsync(companyOwnerId, cancellationToken);

            var canAddEmployee = subscription.Plan!.HasUnlimitedEmployees || 
                                 subscription.Plan.CanAddEmployee(employeeCount);
            var canAddBranch = subscription.Plan.HasUnlimitedBranches || 
                               subscription.Plan.CanAddBranch(branchCount);

            return new TenantSubscriptionDto(
                subscription.Id,
                subscription.HeadquartersId,
                subscription.OwnerId,
                subscription.Plan.Name,
                subscription.Status.ToString(),
                subscription.Plan.MaxEmployees,
                subscription.Plan.MaxBranches,
                employeeCount,
                branchCount,
                subscription.CurrentPeriodEnd,
                canAddEmployee && subscription.IsActiveOrTrialing,
                canAddBranch && subscription.IsActiveOrTrialing);
        }
    }
}
