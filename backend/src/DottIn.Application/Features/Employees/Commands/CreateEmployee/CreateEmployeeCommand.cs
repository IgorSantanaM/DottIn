using DottIn.Application.Shared.DTOS;
using MediatR;

namespace DottIn.Application.Features.Employees.Commands.CreateEmployee;

public record CreateEmployeeCommand(
    string Name,
    DocumentDto Document,
    Stream ImageStream,
    string ImageContentType,
    Guid BranchId,
    TimeOnly StartWorkTime,
    TimeOnly EndWorkTime,
    TimeOnly IntervalStart,
    TimeOnly IntervalEnd)
    : IRequest<Guid>;