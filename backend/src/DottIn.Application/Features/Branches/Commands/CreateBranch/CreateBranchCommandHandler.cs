using DottIn.Application.Exceptions;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;
using DottIn.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace DottIn.Application.Features.Branches.Commands.CreateBranch
{
    public class CreateBranchCommandHandler(IBranchRepository branchRepository,
        IValidator<CreateBranchCommand> validator,
        IUnitOfWork unitOfWork,
        IEmployeeRepository employeeRepository) : IRequestHandler<CreateBranchCommand, Guid>
    {
        public async Task<Guid> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);


            var employee = await employeeRepository.GetByIdAsync(request.OwnerId, cancellationToken);

            if (employee is null)
                throw NotFoundException.ForEntity(nameof(Employee), request.OwnerId);

            if (!employee.IsActive)
                throw new DomainException("O Funcionário não esta ativo.");

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
                            request.OwnerId,
                            request.Email,
                            request.PhoneNumber,
                            request.IsHeadQuarters,
                            request.AllowedRadiusMeters,
                            request.ToleranceMinutes);

            await branchRepository.AddAsync(branch, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return branch.Id;
        }
    }
}
