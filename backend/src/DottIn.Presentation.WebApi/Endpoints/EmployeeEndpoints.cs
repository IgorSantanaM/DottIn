using DottIn.Application.Features.Employees.Commands.ActivateEmployee;
using DottIn.Application.Features.Employees.Commands.CreateEmployee;
using DottIn.Application.Features.Employees.Commands.DeactivateEmployee;
using DottIn.Application.Features.Employees.Commands.UpdateProfile;
using DottIn.Application.Features.Employees.Commands.UpdateSchedule;
using DottIn.Application.Features.Employees.DTOs;
using DottIn.Application.Features.Employees.Queries.GetActiveEmployees;
using DottIn.Application.Features.Employees.Queries.GetEmployeeByCPF;
using DottIn.Application.Features.Employees.Queries.GetEmployeeById;
using DottIn.Application.Features.Employees.Queries.GetEmployeesByBranch;
using DottIn.Application.Shared.DTOS;
using DottIn.Presentation.WebApi.DTOs.Employees;
using DottIn.Presentation.WebApi.Endpoints.Internal;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DottIn.Presentation.WebApi.Endpoints
{
    public class EmployeeEndpoints : IEndpoint
    {
        private const string Tag = "Employees";

        public static void DefineEndpoints(WebApplication app)
        {
            var group = app.MapGroup("/api/branches/{branchId:guid}/employees")
                .WithTags(Tag);

            group.MapGet("/", HandleGetEmployeesByBranchAsync)
                .WithName(nameof(HandleGetEmployeesByBranchAsync))
                .WithSummary("Get all employees by branch")
                .WithDescription("Returns all employees for a specific branch.")
                .Produces<IEnumerable<EmployeeSummaryDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapGet("/active", HandleGetActiveEmployeesAsync)
                .WithName(nameof(HandleGetActiveEmployeesAsync))
                .WithSummary("Get active employees")
                .WithDescription("Returns all active employees for a specific branch.")
                .Produces<IEnumerable<EmployeeSummaryDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapGet("/{employeeId:guid}", HandleGetEmployeeByIdAsync)
                .WithName(nameof(HandleGetEmployeeByIdAsync))
                .WithSummary("Get employee by ID")
                .WithDescription("Returns detailed information about a specific employee.")
                .Produces<EmployeeSummaryDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapGet("/cpf/{cpf}", HandleGetEmployeeByCpfAsync)
                .WithName(nameof(HandleGetEmployeeByCpfAsync))
                .WithSummary("Get employee by CPF")
                .WithDescription("Returns employee information by their CPF document.")
                .Produces<EmployeeSummaryDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapPost("/", HandleCreateEmployeeAsync)
                .WithName(nameof(HandleCreateEmployeeAsync))
                .WithSummary("Create a new employee")
                .WithDescription("Creates a new employee for a branch.")
                .Accepts<CreateEmployeeRequest>("multipart/form-data")
                .Produces<Guid>(StatusCodes.Status201Created)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status422UnprocessableEntity)
                .Produces(StatusCodes.Status500InternalServerError)
                .DisableAntiforgery();

            group.MapPut("/{employeeId:guid}/profile", HandleUpdateProfileAsync)
                .WithName(nameof(HandleUpdateProfileAsync))
                .WithSummary("Update employee profile")
                .WithDescription("Updates the name and optionally the image of an employee.")
                .Accepts<UpdateProfileRequest>("multipart/form-data")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError)
                .DisableAntiforgery();

            group.MapPut("/{employeeId:guid}/schedule", HandleUpdateEmployeeScheduleAsync)
                .WithName(nameof(HandleUpdateEmployeeScheduleAsync))
                .WithSummary("Update employee schedule")
                .WithDescription("Updates the work schedule and break times of an employee.")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapPatch("/{employeeId:guid}/activate", HandleActivateEmployeeAsync)
                .WithName(nameof(HandleActivateEmployeeAsync))
                .WithSummary("Activate employee")
                .WithDescription("Activates a deactivated employee.")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapPatch("/{employeeId:guid}/deactivate", HandleDeactivateEmployeeAsync)
                .WithName(nameof(HandleDeactivateEmployeeAsync))
                .WithSummary("Deactivate employee")
                .WithDescription("Deactivates an employee. They won't be able to clock in.")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);
        }

        #region Query Handlers

        private static async Task<IResult> HandleGetEmployeesByBranchAsync(
            [FromRoute] Guid branchId,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetEmployeesByBranchQuery(branchId);
            var employees = await mediator.Send(query, cancellationToken);
            return Results.Ok(employees);
        }

        private static async Task<IResult> HandleGetActiveEmployeesAsync(
            [FromRoute] Guid branchId,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetActiveEmployeesQuery(branchId);
            var employees = await mediator.Send(query, cancellationToken);
            return Results.Ok(employees);
        }

        private static async Task<IResult> HandleGetEmployeeByIdAsync(
            [FromRoute] Guid branchId,
            [FromRoute] Guid employeeId,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetEmployeeByIdQuery(branchId, employeeId);
            var employee = await mediator.Send(query, cancellationToken);

            return employee is null
                ? Results.NotFound($"Employee with ID {employeeId} not found.")
                : Results.Ok(employee);
        }

        private static async Task<IResult> HandleGetEmployeeByCpfAsync(
            [FromRoute] Guid branchId,
            [FromRoute] string cpf,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetEmployeeByCPFQuery(branchId, cpf);
            var employee = await mediator.Send(query, cancellationToken);

            return employee is null
                ? Results.NotFound($"Employee with CPF {cpf} not found.")
                : Results.Ok(employee);
        }

        #endregion

        #region Command Handlers

        private static async Task<IResult> HandleCreateEmployeeAsync(
            [FromRoute] Guid branchId,
            [FromForm] CreateEmployeeRequest request,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var imageStream = request.Image.OpenReadStream();

            var command = new CreateEmployeeCommand(
                request.Name,
                new DocumentDto(request.Cpf, Domain.ValueObjects.DocumentType.CPF),
                imageStream,
                request.Image.ContentType,
                branchId,
                request.StartWorkTime,
                request.EndWorkTime,
                request.IntervalStart,
                request.IntervalEnd);

            var employeeId = await mediator.Send(command, cancellationToken);

            return employeeId == Guid.Empty
                ? Results.BadRequest("Failed to create employee.")
                : Results.CreatedAtRoute(nameof(HandleGetEmployeeByIdAsync), new { branchId, employeeId }, employeeId);
        }

        private static async Task<IResult> HandleUpdateProfileAsync(
            [FromRoute] Guid branchId,
            [FromRoute] Guid employeeId,
            [FromForm] UpdateProfileRequest request,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            Stream? imageStream = null;
            string? contentType = null;

            if (request.Image is not null)
            {
                imageStream = request.Image.OpenReadStream();
                contentType = request.Image.ContentType;
            }

            var command = new UpdateProfileCommand(
                employeeId,
                branchId,
                request.Name,
                imageStream,
                contentType);

            await mediator.Send(command, cancellationToken);
            return Results.NoContent();
        }

        private static async Task<IResult> HandleUpdateEmployeeScheduleAsync(
            [FromRoute] Guid branchId,
            [FromRoute] Guid employeeId,
            [FromBody] UpdateEmployeeScheduleRequest request,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var command = new UpdateScheduleCommand(
                branchId,
                employeeId,
                request.Start,
                request.End,
                request.IntervalStart,
                request.IntervalEnd);

            await mediator.Send(command, cancellationToken);
            return Results.NoContent();
        }

        private static async Task<IResult> HandleActivateEmployeeAsync(
            [FromRoute] Guid branchId,
            [FromRoute] Guid employeeId,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var command = new ActivateEmployeeCommand(branchId, employeeId);
            await mediator.Send(command, cancellationToken);
            return Results.NoContent();
        }

        private static async Task<IResult> HandleDeactivateEmployeeAsync(
            [FromRoute] Guid branchId,
            [FromRoute] Guid employeeId,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var command = new DeactivateEmployeeCommand(branchId, employeeId);
            await mediator.Send(command, cancellationToken);
            return Results.NoContent();
        }

        #endregion
    }
}
