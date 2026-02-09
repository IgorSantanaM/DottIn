using DottIn.Application.Exceptions;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace DottIn.Application.Features.Branches.Commands.MoveLocation
{
    public class MoveLocationCommandHandle(IBranchRepository branchRepository,
        IValidator<MoveLocationCommand> validator,
        IUnitOfWork unitOfWork)
        : IRequestHandler<MoveLocationCommand, Unit>
    {
        public async Task<Unit> Handle(MoveLocationCommand request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);

            var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A empresa não esta ativa.");

            var address = new Address(request.NewAddress.Street,
                request.NewAddress.Number,
                request.NewAddress.City,
                request.NewAddress.State,
                request.NewAddress.ZipCode,
                request.NewAddress.Complement);

            var geolocation = new Geolocation(request.NewGeolocation.Latitude, request.NewGeolocation.Longitude);

            branch.MoveLocation(address, geolocation, request.NewTimeZoneId);

            await branchRepository.UpdateAsync(branch);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
