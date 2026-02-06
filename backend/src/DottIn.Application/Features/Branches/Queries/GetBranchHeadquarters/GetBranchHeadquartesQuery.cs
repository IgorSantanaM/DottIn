using DottIn.Application.Features.Branches.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace DottIn.Application.Features.Branches.Queries.GetBranchHeadquarters;

public record GetBranchHeadquartesQuery : IRequest<IEnumerable<BranchSummaryDto>>;
