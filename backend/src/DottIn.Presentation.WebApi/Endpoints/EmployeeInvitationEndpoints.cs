using System.Data;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using DottIn.Application.Features.Subscriptions.Services;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Employees;
using DottIn.Domain.ValueObjects;
using DottIn.Infra.Data.Contexts;
using DottIn.Presentation.WebApi.DTOs.Employees;
using DottIn.Presentation.WebApi.Endpoints.Internal;
using DottIn.Presentation.WebApi.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Presentation.WebApi.Endpoints;

public sealed class EmployeeInvitationEndpoints : IEndpoint
{
    private const string Tag = "Employee invitations";

    public static void DefineEndpoints(WebApplication app)
    {
        var protectedGroup = app.MapGroup("/api/branches/{branchId:guid}/employee-invitations")
            .WithTags(Tag)
            .RequireAuthorization()
            .AddEndpointFilter<TenantAuthorizationFilter>();

        protectedGroup.MapGet("/", HandleListAsync)
            .Produces<IEnumerable<EmployeeInvitationResponse>>(StatusCodes.Status200OK);

        protectedGroup.MapPost("/", HandleCreateAsync)
            .Produces<EmployeeInvitationCreatedResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status409Conflict);

        protectedGroup.MapDelete("/{invitationId:guid}", HandleRevokeAsync)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/employee-invitations/accept", HandleAcceptAsync)
            .WithTags(Tag)
            .AllowAnonymous()
            .RequireRateLimiting("public-auth")
            .Produces<AcceptEmployeeInvitationResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleListAsync(
        [FromRoute] Guid branchId,
        [FromServices] IEmployeeInvitationRepository repository,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var invitations = await repository.GetByBranchAsync(branchId, cancellationToken);
        return Results.Ok(invitations.Select(x => ToResponse(x, now)));
    }

    private static async Task<IResult> HandleCreateAsync(
        [FromRoute] Guid branchId,
        [FromBody] CreateEmployeeInvitationRequest request,
        [FromServices] CurrentUserContext currentUser,
        [FromServices] IEmployeeInvitationRepository invitationRepository,
        [FromServices] ITenantSubscriptionService subscriptionService,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsManager)
            return Results.Forbid();
        if (request.Role == EmployeeRole.Owner ||
            currentUser.Role == EmployeeRole.Manager && request.Role != EmployeeRole.Employee)
            return Results.Forbid();
        if (request.ExpiresInHours is < 1 or > 168)
            return Results.BadRequest(new { Message = "A validade deve ficar entre 1 e 168 horas." });
        if (!string.IsNullOrWhiteSpace(request.Email) && !MailAddress.TryCreate(request.Email, out _))
            return Results.BadRequest(new { Message = "E-mail inválido." });

        var subscription = await subscriptionService.GetByOwnerIdAsync(currentUser.TenantId, cancellationToken);
        if (subscription is null)
            return Results.Conflict(new { Message = "Assinatura não configurada." });

        var pending = await invitationRepository.CountPendingByOwnerIdAsync(
            currentUser.TenantId, DateTime.UtcNow, cancellationToken);
        if (subscription.MaxEmployees != -1 &&
            subscription.CurrentEmployeeCount + pending >= subscription.MaxEmployees)
        {
            return Results.Conflict(new { Message = "O limite de assentos do plano foi atingido." });
        }

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var now = DateTime.UtcNow;
        var invitation = new EmployeeInvitation(
            branchId,
            currentUser.EmployeeId,
            HashToken(rawToken),
            request.Role,
            now.AddHours(request.ExpiresInHours),
            now,
            request.Email);

        await invitationRepository.AddAsync(invitation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/branches/{branchId}/employee-invitations/{invitation.Id}",
            new EmployeeInvitationCreatedResponse(invitation.Id, rawToken, invitation.ExpiresAt));
    }

    private static async Task<IResult> HandleRevokeAsync(
        [FromRoute] Guid branchId,
        [FromRoute] Guid invitationId,
        [FromServices] CurrentUserContext currentUser,
        [FromServices] IEmployeeInvitationRepository repository,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsManager)
            return Results.Forbid();

        var invitation = await repository.GetByIdAsync(invitationId, cancellationToken);
        if (invitation is null || invitation.BranchId != branchId)
            return Results.NotFound();

        invitation.Revoke(DateTime.UtcNow);
        await repository.UpdateAsync(invitation);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> HandleAcceptAsync(
        [FromBody] AcceptEmployeeInvitationRequest request,
        [FromServices] DottInContext db,
        [FromServices] IEmployeeInvitationRepository invitationRepository,
        [FromServices] IEmployeeRepository employeeRepository,
        [FromServices] IBranchRepository branchRepository,
        [FromServices] ITenantSubscriptionService subscriptionService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Results.BadRequest(new { Message = "Token obrigatório." });

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var invitation = await invitationRepository.GetByTokenHashAsync(HashToken(request.Token), cancellationToken);
        if (invitation is null || invitation.StatusAt(DateTime.UtcNow) != InvitationStatus.Pending)
            return Results.BadRequest(new { Message = "Convite inválido ou expirado." });

        var branch = await branchRepository.GetByIdAsync(invitation.BranchId, cancellationToken);
        if (branch is null || !branch.IsActive || !branch.OwnerId.HasValue)
            return Results.BadRequest(new { Message = "Filial indisponível." });

        if (!await subscriptionService.CanAddEmployeeAsync(branch.OwnerId.Value, cancellationToken))
            return Results.Conflict(new { Message = "O limite de assentos do plano foi atingido." });

        var document = new Document(request.Cpf);
        if (await employeeRepository.GetByCPFAsync(document.Value, cancellationToken) is not null)
            return Results.Conflict(new { Message = "Já existe um funcionário com este CPF." });

        var employee = new Employee(
            request.Name,
            document,
            branch.Id,
            request.StartWorkTime,
            request.EndWorkTime,
            request.IntervalStart,
            request.IntervalEnd);
        employee.SetRole(invitation.Role);
        employee.SetPassword(request.Password);

        await employeeRepository.AddAsync(employee, cancellationToken);
        invitation.Consume(employee.Id, DateTime.UtcNow);
        await invitationRepository.UpdateAsync(invitation);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Results.Conflict(new { Message = "O convite foi utilizado ou os dados já existem." });
        }

        return Results.Created(
            $"/api/branches/{branch.Id}/employees/{employee.Id}",
            new AcceptEmployeeInvitationResponse(employee.Id, branch.Id));
    }

    private static EmployeeInvitationResponse ToResponse(EmployeeInvitation invitation, DateTime now)
        => new(
            invitation.Id,
            invitation.BranchId,
            invitation.Email,
            invitation.Role,
            invitation.StatusAt(now),
            invitation.CreatedAt,
            invitation.ExpiresAt,
            invitation.ConsumedAt,
            invitation.RevokedAt);

    private static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
