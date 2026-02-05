using DottIn.Application.Exceptions;
using DottIn.Application.Features.Employees.Events;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;
using FluentValidation;
using MassTransit;
using MediatR;

namespace DottIn.Application.Features.Employees.Commands.UpdateProfile
{
    public class UpdateProfileCommandHandler(IPublishEndpoint publishEndpoint,
                        IBranchRepository branchRepository,
                        IEmployeeRepository employeeRepository,
                        IUnitOfWork unitOfWork,
                        IValidator<UpdateProfileCommand> validator)
                        : IRequestHandler<UpdateProfileCommand, bool>
    {
        public async Task<bool> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);

            var branch = await branchRepository.GetByIdAsync(request.BranchId);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A empresa não esta ativa.");

            var employee = await employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);

            if (employee is null)
                throw NotFoundException.ForEntity(nameof(Employee), request.EmployeeId);

            if (employee.IsActive)
                return true;

            employee.UpdateProfile(request.Name);

            await employeeRepository.UpdateAsync(employee);

            if (request.EmployeeImage is not null && !string.IsNullOrEmpty(request.ImageContentType))
            {
                var imageName = employee.Name + request.EmployeeId;
                var employeeImageUpdated = new EmployeeImageUpdated(request.EmployeeId, request.EmployeeImage, imageName, request.ImageContentType);
                await publishEndpoint.Publish(employeeImageUpdated, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
