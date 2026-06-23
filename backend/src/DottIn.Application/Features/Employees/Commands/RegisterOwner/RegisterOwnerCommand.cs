using DottIn.Application.Shared.DTOS;
using MediatR;

namespace DottIn.Application.Features.Employees.Commands.RegisterOwner;

public record RegisterOwnerCommand(
    string Name,
    DocumentDto Document,
    string Password)
    : IRequest<Guid>;
