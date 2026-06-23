using DottIn.Domain.Core.Data;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;
using DottIn.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace DottIn.Application.Features.Employees.Commands.RegisterOwner;

public class RegisterOwnerCommandHandler(
    IEmployeeRepository employeeRepository,
    IValidator<RegisterOwnerCommand> validator,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterOwnerCommand, Guid>
{
    public async Task<Guid> Handle(RegisterOwnerCommand request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var document = new Document(request.Document.Value);

        // Check for duplicate CPF
        var existing = await employeeRepository.GetByCPFAsync(document.Value, cancellationToken);
        if (existing is not null)
            throw new DomainException("Já existe uma conta registrada com este CPF.");

        var employee = new Employee(request.Name, document, request.Password);

        await employeeRepository.AddAsync(employee);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return employee.Id;
    }
}
