using DottIn.Application.Features.Branches.DTOs;
using MediatR;

namespace DottIn.Application.Features.Branches.Queries.GetBranchByDocument;

public record GetBranchByDocumentQuery(string Document) : IRequest<BranchDetailsDto>;
