using DottIn.Application.Features.TimeKeepings.DTOs;
using MediatR;

namespace DottIn.Application.Features.TimeKeepings.Queries.GetTimeKeepingById;

public record GetTimeKeepingByIdQuery(Guid TimeKeepingId) : IRequest<TimeKeepingDetailsDto>;
