using DottIn.Application.Features.Branches.Commands.ActivateBranch;
using DottIn.Application.Features.Branches.Commands.CreateBranch;
using DottIn.Application.Features.Branches.Commands.DeactivateBranch;
using DottIn.Application.Features.Branches.Commands.MoveLocation;
using DottIn.Application.Features.Branches.Commands.SetComplianceRules;
using DottIn.Application.Features.Branches.Commands.SetOwner;
using DottIn.Application.Features.Branches.Commands.UpdateConfig;
using DottIn.Application.Features.Branches.Commands.UpdateSchedule;
using DottIn.Application.Features.Branches.DTOs;
using DottIn.Application.Features.Branches.Queries.GetActiveBranches;
using DottIn.Application.Features.Branches.Queries.GetBranchByDocument;
using DottIn.Application.Features.Branches.Queries.GetBranchById;
using DottIn.Application.Features.Branches.Queries.GetBranchByOwner;
using DottIn.Application.Features.Branches.Queries.GetBranchHeadquarters;
using DottIn.Application.Shared.DTOS;
using DottIn.Presentation.WebApi.DTOs.Branches;
using DottIn.Presentation.WebApi.Endpoints.Internal;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DottIn.Presentation.WebApi.Endpoints
{
    public class BranchEndpoints : IEndpoint
    {
        private const string Tag = "Branches";

        public static void DefineEndpoints(WebApplication app)
        {
            var group = app.MapGroup("/api/branches")
                .WithTags(Tag);

            group.MapGet("/", HandleGetActiveBranchesAsync)
                .WithName(nameof(HandleGetActiveBranchesAsync))
                .WithSummary("Get all active branches")
                .WithDescription("Returns a list of all active branches in the system.")
                .Produces<IEnumerable<BranchSummaryDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapGet("/{branchId:guid}", HandleGetBranchByIdAsync)
                .WithName(nameof(HandleGetBranchByIdAsync))
                .WithSummary("Get branch by ID")
                .WithDescription("Returns detailed information about a specific branch.")
                .Produces<BranchDetailsDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapGet("/document/{document}", HandleGetBranchByDocumentAsync)
                .WithName(nameof(HandleGetBranchByDocumentAsync))
                .WithSummary("Get branch by document (CNPJ)")
                .WithDescription("Returns detailed information about a branch by its CNPJ document.")
                .Produces<BranchDetailsDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapGet("/owner/{ownerId:guid}", HandleGetBranchesByOwnerAsync)
                .WithName(nameof(HandleGetBranchesByOwnerAsync))
                .WithSummary("Get branches by owner")
                .WithDescription("Returns all branches owned by a specific employee.")
                .Produces<IEnumerable<BranchSummaryDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapGet("/headquarters", HandleGetHeadquartersAsync)
                .WithName(nameof(HandleGetHeadquartersAsync))
                .WithSummary("Get headquarters")
                .WithDescription("Returns all branches marked as headquarters.")
                .Produces<IEnumerable<BranchSummaryDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapPost("/", HandleCreateBranchAsync)
                .WithName(nameof(HandleCreateBranchAsync))
                .WithSummary("Create a new branch")
                .WithDescription("Creates a new branch/company in the system.")
                .Produces<Guid>(StatusCodes.Status201Created)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status422UnprocessableEntity)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapPut("/{branchId:guid}/location", HandleMoveLocationAsync)
                .WithName(nameof(HandleMoveLocationAsync))
                .WithSummary("Update branch location")
                .WithDescription("Updates the address and geolocation of a branch.")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapPut("/{branchId:guid}/schedule", HandleUpdateScheduleAsync)
                .WithName(nameof(HandleUpdateScheduleAsync))
                .WithSummary("Update branch schedule")
                .WithDescription("Updates the work schedule (start and end times) of a branch.")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapPut("/{branchId:guid}/config", HandleUpdateConfigAsync)
                .WithName(nameof(HandleUpdateConfigAsync))
                .WithSummary("Update branch configuration")
                .WithDescription("Updates branch configuration like allowed radius and timezone.")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapPut("/{branchId:guid}/compliance", HandleSetComplianceRulesAsync)
                .WithName(nameof(HandleSetComplianceRulesAsync))
                .WithSummary("Set compliance rules")
                .WithDescription("Sets compliance rules like tolerance minutes and holiday calendar.")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapPut("/{branchId:guid}/owner", HandleSetOwnerAsync)
                .WithName(nameof(HandleSetOwnerAsync))
                .WithSummary("Set branch owner")
                .WithDescription("Assigns an employee as the owner of a branch.")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapPatch("/{branchId:guid}/activate", HandleActivateBranchAsync)
                .WithName(nameof(HandleActivateBranchAsync))
                .WithSummary("Activate branch")
                .WithDescription("Activates a deactivated branch.")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapPatch("/{branchId:guid}/deactivate", HandleDeactivateBranchAsync)
                .WithName(nameof(HandleDeactivateBranchAsync))
                .WithSummary("Deactivate branch")
                .WithDescription("Deactivates an active branch. Employees won't be able to clock in.")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);
        }

        #region Query Handlers

        private static async Task<IResult> HandleGetActiveBranchesAsync(
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetActiveBranchesQuery();
            var branches = await mediator.Send(query, cancellationToken);
            return Results.Ok(branches);
        }

        private static async Task<IResult> HandleGetBranchByIdAsync(
            [FromRoute] Guid branchId,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetBranchByIdQuery(branchId);
            var branch = await mediator.Send(query, cancellationToken);

            return branch is null
                ? Results.NotFound($"Branch with ID {branchId} not found.")
                : Results.Ok(branch);
        }

        private static async Task<IResult> HandleGetBranchByDocumentAsync(
            [FromRoute] string document,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetBranchByDocumentQuery(document);
            var branch = await mediator.Send(query, cancellationToken);

            return branch is null
                ? Results.NotFound($"Branch with document {document} not found.")
                : Results.Ok(branch);
        }

        private static async Task<IResult> HandleGetBranchesByOwnerAsync(
            [FromRoute] Guid ownerId,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetBranchByOwnerQuery(ownerId);
            var branches = await mediator.Send(query, cancellationToken);
            return Results.Ok(branches);
        }

        private static async Task<IResult> HandleGetHeadquartersAsync(
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetBranchHeadquartesQuery();
            var branches = await mediator.Send(query, cancellationToken);
            return Results.Ok(branches);
        }

        #endregion

        #region Command Handlers

        private static async Task<IResult> HandleCreateBranchAsync(
            [FromBody] CreateBranchCommand command,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var branchId = await mediator.Send(command, cancellationToken);

            return branchId == Guid.Empty
                ? Results.BadRequest("Failed to create branch.")
                : Results.CreatedAtRoute(nameof(HandleGetBranchByIdAsync), new { branchId }, branchId);
        }

        private static async Task<IResult> HandleMoveLocationAsync(
            [FromRoute] Guid branchId,
            [FromBody] MoveLocationRequest request,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var command = new MoveLocationCommand(
                branchId,
                request.NewAddress,
                request.NewGeolocation,
                request.NewTimeZoneId);

            await mediator.Send(command, cancellationToken);
            return Results.NoContent();
        }

        private static async Task<IResult> HandleUpdateScheduleAsync(
            [FromRoute] Guid branchId,
            [FromBody] UpdateScheduleRequest request,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var command = new UpdateScheduleCommand(branchId, request.Start, request.End);
            await mediator.Send(command, cancellationToken);
            return Results.NoContent();
        }

        private static async Task<IResult> HandleUpdateConfigAsync(
            [FromRoute] Guid branchId,
            [FromBody] UpdateConfigRequest request,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var command = new UpdateConfigCommand(branchId, request.AllowedRadiusMeters, request.TimeZoneId);
            await mediator.Send(command, cancellationToken);
            return Results.NoContent();
        }

        private static async Task<IResult> HandleSetComplianceRulesAsync(
            [FromRoute] Guid branchId,
            [FromBody] SetComplianceRulesRequest request,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var command = new SetComplianceRulesCommand(branchId, request.ToleranceMinutes, request.HolidayCalendarId);
            await mediator.Send(command, cancellationToken);
            return Results.NoContent();
        }

        private static async Task<IResult> HandleSetOwnerAsync(
            [FromRoute] Guid branchId,
            [FromBody] SetOwnerRequest request,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var command = new SetOwnerCommand(branchId, request.EmployeeId);
            await mediator.Send(command, cancellationToken);
            return Results.NoContent();
        }

        private static async Task<IResult> HandleActivateBranchAsync(
            [FromRoute] Guid branchId,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var command = new ActivateBranchCommand(branchId);
            await mediator.Send(command, cancellationToken);
            return Results.NoContent();
        }

        private static async Task<IResult> HandleDeactivateBranchAsync(
            [FromRoute] Guid branchId,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var command = new DeactivateBranchCommand(branchId);
            await mediator.Send(command, cancellationToken);
            return Results.NoContent();
        }

        #endregion
    }
}
