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

            if (!request.OwnerId.HasValue || request.OwnerId.Value == Guid.Empty)
                throw new DomainException("Informe o proprietário da empresa.");

            var result = Guid.Empty;
            await unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                await branchRepository.LockOwnerAsync(request.OwnerId.Value, token);
                var document = new string(request.Document.Value.Where(char.IsDigit).ToArray());
                await branchRepository.LockDocumentAsync(document, token);
                if (await branchRepository.GetByDocumentAsync(document, token) is not null)
                    throw new DomainException("Já existe uma empresa cadastrada com este CNPJ.");
                // Never trust the headquarters flag from the browser or a stale pre-transaction check.
                var existing = await branchRepository.GetByOwnerIdAsync(request.OwnerId.Value, token);
                result = await CreateAsync(request with { IsHeadQuarters = !existing.Any() }, token);
            }, cancellationToken);
            return result;
        }

        private async Task<Guid> CreateAsync(CreateBranchCommand request, CancellationToken cancellationToken)
        {

            Employee? owner = null;
            if (request.OwnerId.HasValue && request.OwnerId.Value != Guid.Empty)
            {
                owner = await employeeRepository.GetByIdAsync(request.OwnerId.Value, cancellationToken);

                if (owner is null)
                    throw NotFoundException.ForEntity(nameof(Employee), request.OwnerId.Value);

                if (!owner.IsActive)
                    throw new DomainException("O funcionário não está ativo.");
                if (owner.Role != EmployeeRole.Owner)
                    throw new DomainException("Somente o proprietário pode cadastrar filiais.");
            }

            // Recheck a distinct billing owner inside the transaction as well: an earlier
            // authorization check must not survive removal of that ownership while waiting for the lock.
            if (request.CreatedByEmployeeId.HasValue && request.CreatedByEmployeeId != owner?.Id)
            {
                var creator = await employeeRepository.GetByIdAsync(request.CreatedByEmployeeId.Value, cancellationToken);
                var headquarters = (await branchRepository.GetByOwnerIdAsync(request.OwnerId!.Value, cancellationToken))
                    .FirstOrDefault(b => b.IsHeadquarters);
                var subscription = headquarters is null ? null
                    : await tenantSubscriptionRepository.GetByHeadquartersIdAsync(headquarters.Id, cancellationToken);
                if (creator is null || !creator.IsActive || creator.Role != EmployeeRole.Owner ||
                    request.IsHeadQuarters || subscription?.OwnerId != creator.Id)
                    throw new DomainException("Somente o proprietário da matriz ou da assinatura vinculada pode cadastrar filiais.");
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
                        subscription is null || subscription.Status is not ("Free" or "Active" or "Trialing")
                            ? "A assinatura não está elegível para criar filiais. Confira seu plano e o status da assinatura."
                            : $"O limite do plano foi atingido ({maxBranches} unidades, incluindo a matriz).");
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
                            request.ToleranceMinutes,
                            createdByEmployeeId: request.CreatedByEmployeeId ?? owner?.Id);

            await branchRepository.AddAsync(branch, cancellationToken);

            // If this is a Headquarters with an owner, create Stripe customer and Free subscription
            if (request.IsHeadQuarters && owner != null)
            {
                await CreateTenantSubscriptionAsync(branch, owner, cancellationToken);
            }

            if (request.IsHeadQuarters && owner is not null && owner.BranchId == Guid.Empty)
            {
                // Employee.BranchId is an alternate key used by tenant-safe foreign keys.
                // Persist the branch first, then update the owner directly in one transaction.
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await employeeRepository.AssociateUnassignedOwnerWithBranchAsync(owner.Id, branch.Id, cancellationToken);
            }
            else
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

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
