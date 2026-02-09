using DottIn.Application.Exceptions;
using DottIn.Application.Features.Employees.Events;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;
using DottIn.Domain.ValueObjects;
using FluentValidation;
using MassTransit;
using MediatR;

namespace DottIn.Application.Features.Employees.Commands.CreateEmployee
{
    public class CreateEmployeeCommandHandler(IEmployeeRepository employeeRepository,
                    IBranchRepository branchRepository,
                    IValidator<CreateEmployeeCommand> validator,
                    IPublishEndpoint publishEndpoint,
                    IUnitOfWork unitOfWork)
                    : IRequestHandler<CreateEmployeeCommand, Guid>
    {
        public async Task<Guid> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);

            var branch = await branchRepository.GetByIdAsync(request.BranchId);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A empresa não esta ativa.");

            var document = new Document(request.Document.Value);

            var employee = new Employee(request.Name,
                                document,
                                request.BranchId,
                                request.StartWorkTime,
                                request.EndWorkTime,
                                request.IntervalStart,
                                request.IntervalEnd);

            await employeeRepository.AddAsync(employee);

            var imageName = employee.Name + employee.Id;
            var employteeImageAdded = new EmployeeImageAdded(employee.Id,
                                request.ImageStream,
                                imageName,
                                request.ImageContentType);

            await publishEndpoint.Publish(employteeImageAdded, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }
}
