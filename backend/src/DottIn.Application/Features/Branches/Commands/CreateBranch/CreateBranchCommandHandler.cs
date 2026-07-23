using DottIn.Application.Exceptions;
using DottIn.Application.Features.Subscriptions.Services;
using DottIn.Application.Interfaces;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;
using DottIn.Domain.Subscriptions;
using DottIn.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace DottIn.Application.Features.Branches.Commands.CreateBranch
{
    public class CreateBranchCommandHandler(IBranchRepository branchRepository,
        IValidator<CreateBranchCommand> validator,
        IUnitOfWork unitOfWork,
        IEmployeeRepository employeeRepository,
        IStripeService stripeService,
        ISubscriptionPlanRepository subscriptionPlanRepository,
        ITenantSubscriptionRepository tenantSubscriptionRepository,
        ITenantSubscriptionService tenantSubscriptionService) : IRequestHandler<CreateBranchCommand, Guid>
    {
        public async Task<Guid> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);

            Employee? owner = null;
            if (request.OwnerId.HasValue && request.OwnerId.Value != Guid.Empty)
            {
                owner = await employeeRepository.GetByIdAsync(request.OwnerId.Value, cancellationToken);

                if (owner is null)
                    throw NotFoundException.ForEntity(nameof(Employee), request.OwnerId.Value);

                if (!owner.IsActive)
                    throw new DomainException("O funcionário não está ativo.");
            }

            // Check branch limit if this is NOT a headquarters (HQ is always allowed as it's the first branch)
            if (!request.IsHeadQuarters && owner != null)
            {
                var canAddBranch = await tenantSubscriptionService.CanAddBranchAsync(owner.Id, cancellationToken);
                if (!canAddBranch)
                {
                    var subscription = await tenantSubscriptionService.GetByOwnerIdAsync(owner.Id, cancellationToken);
                    var maxBranches = subscription?.MaxBranches ?? 1;
                    throw new SubscriptionLimitExceededException(
                        $"O limite de filiais do plano foi atingido ({maxBranches} filiais).");
                }
            }

            var document = new Document(request.Document.Value);
            var geolocation = new Geolocation(request.Geolocation.Latitude, request.Geolocation.Longitude);

            var address = new Address(request.Address.Street,
                request.Address.Number,
                request.Address.City,
                request.Address.State,
                request.Address.ZipCode,
                request.Address.Complement);

            var branch = new Branch(request.Name,
                            document,
                            geolocation,
                            address,
                            request.TimeZoneId,
                            request.StartWorkTime,
                            request.EndWorkTime,
                            request.OwnerId ?? Guid.Empty,
                            request.Email,
                            request.PhoneNumber,
                            request.IsHeadQuarters,
                            request.AllowedRadiusMeters,
                            request.ToleranceMinutes);

            await branchRepository.AddAsync(branch, cancellationToken);

            if (request.IsHeadQuarters && owner is not null && owner.BranchId == Guid.Empty)
            {
                owner.AssociateOwnerWithBranch(branch.Id);
                await employeeRepository.UpdateAsync(owner);
            }

            // If this is a Headquarters with an owner, create Stripe customer and Free subscription
            if (request.IsHeadQuarters && owner != null)
            {
                await CreateTenantSubscriptionAsync(branch, owner, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return branch.Id;
        }

        private async Task CreateTenantSubscriptionAsync(Branch headquarters, Employee owner, CancellationToken cancellationToken)
        {
            // Get the Free plan
            var freePlan = await subscriptionPlanRepository.GetByNameAsync("Free", cancellationToken)
                ?? throw new DomainException("Plano Free não encontrado no sistema.");

            // Use HQ email for Stripe customer, fall back to generated email if not set
            var customerEmail = !string.IsNullOrWhiteSpace(headquarters.Email) 
                ? headquarters.Email 
                : $"hq-{headquarters.Id}@dottin.app";

            // Create Stripe customer
            var stripeCustomerId = await stripeService.CreateCustomerAsync(
                customerEmail,
                headquarters.Name,
                headquarters.Id);

            // Create TenantSubscription with Free plan
            var subscription = new TenantSubscription(
                headquartersId: headquarters.Id,
                ownerId: owner.Id,
                stripeCustomerId: stripeCustomerId,
                subscriptionPlanId: freePlan.Id);

            await tenantSubscriptionRepository.AddAsync(subscription, cancellationToken);
        }
    }
}
