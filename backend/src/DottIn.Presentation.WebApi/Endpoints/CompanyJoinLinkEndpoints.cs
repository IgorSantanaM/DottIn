using System.Data;
using DottIn.Domain.Auth;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Employees;
using DottIn.Domain.Subscriptions;
using DottIn.Domain.ValueObjects;
using DottIn.Infra.Data.Contexts;
using DottIn.Infra.Services.Auth;
using DottIn.Presentation.WebApi.DTOs.Employees;
using DottIn.Presentation.WebApi.Endpoints.Internal;
using DottIn.Presentation.WebApi.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Presentation.WebApi.Endpoints;

public sealed class CompanyJoinLinkEndpoints : IEndpoint
{
    private const int LinkLifetimeDays = 30;

    public static void DefineEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/branches/{branchId:guid}/company-join-link")
            .WithTags("Company join links")
            .RequireAuthorization()
            .AddEndpointFilter<TenantAuthorizationFilter>();

        group.MapGet("/", GetOrCreateAsync)
            .Produces<CompanyJoinLinkResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden);

        app.MapGet("/api/company-join-links/resolve", ResolveAsync)
            .WithTags("Company join links")
            .AllowAnonymous()
            .RequireRateLimiting("public-auth")
            .Produces<CompanyJoinLinkResolutionResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        app.MapPost("/api/company-join-links/register", RegisterAsync)
            .WithTags("Company join links")
            .AllowAnonymous()
            .RequireRateLimiting("public-auth")
            .Produces<RegisterFromCompanyJoinLinkResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> GetOrCreateAsync(
        [FromRoute] Guid branchId,
        [FromServices] CurrentUserContext currentUser,
        [FromServices] DottInContext db,
        [FromServices] ICompanyJoinLinkRepository linkRepository,
        [FromServices] IBranchRepository branchRepository,
        [FromServices] ICompanyJoinLinkTokenService tokenService,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsManager)
            return Results.Forbid();

        var branch = await branchRepository.GetByIdAsync(branchId, cancellationToken);
        if (branch is null || !branch.IsActive || branch.OwnerId != currentUser.TenantId)
            return Results.Forbid();

        var link = await linkRepository.GetByBranchIdAsync(branchId, cancellationToken);
        if (link is null || !link.IsActiveAt(DateTime.UtcNow))
        {
            if (link is not null)
                link.Renew(DateTime.UtcNow.AddDays(LinkLifetimeDays));
            else
            {
                link = new CompanyJoinLink(branchId, currentUser.EmployeeId, DateTime.UtcNow.AddDays(LinkLifetimeDays));
                await linkRepository.AddAsync(link, cancellationToken);
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        return Results.Ok(new CompanyJoinLinkResponse(tokenService.CreateToken(link), link.ExpiresAt, branch.Name));
    }

    private static async Task<IResult> ResolveAsync(
        [FromQuery] string token,
        [FromServices] DottInContext db,
        [FromServices] ICompanyJoinLinkTokenService tokenService,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(token, db, tokenService, cancellationToken);
        if (validation is null)
            return Results.BadRequest(new { Message = "Link de convite inválido ou expirado." });

        var (_, branch, subscription) = validation.Value;
        var employeeCount = await CountBillableEmployeesAsync(db, branch.OwnerId!.Value, cancellationToken);
        var canJoin = subscription.Plan!.HasUnlimitedEmployees || employeeCount < subscription.Plan.MaxEmployees;
        return Results.Ok(new CompanyJoinLinkResolutionResponse(branch.Name, canJoin));
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterFromCompanyJoinLinkRequest request,
        [FromServices] DottInContext db,
        [FromServices] ICompanyJoinLinkTokenService tokenService,
        [FromServices] ITokenService jwtTokenService,
        [FromServices] IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Cpf) || string.IsNullOrWhiteSpace(request.Password))
            return Results.BadRequest(new { Message = "Nome, CPF e senha são obrigatórios." });

        var cpf = new string(request.Cpf.Where(char.IsDigit).ToArray());
        if (cpf.Length != 11)
            return Results.BadRequest(new { Message = "O CPF deve conter 11 dígitos." });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var validation = await ValidateAsync(request.Token, db, tokenService, cancellationToken);
        if (validation is null)
            return Results.BadRequest(new { Message = "Link de convite inválido ou expirado." });

        var (_, branch, subscription) = validation.Value;
        if (await db.Employees.AnyAsync(x => x.CPF.Value == cpf, cancellationToken))
            return Results.Conflict(new { Message = "Já existe uma conta registrada com este CPF. Faça login pelo link." });

        if (!await HasSeatAvailableAsync(db, branch.OwnerId!.Value, subscription, cancellationToken))
            return Results.Conflict(new { Message = "Não há assentos disponíveis nesta empresa." });

        Employee employee;
        try
        {
            var (intervalStart, intervalEnd) = DefaultInterval(branch);
            employee = new Employee(request.Name.Trim(), new Document(cpf), branch.Id,
                branch.StartWorkTime, branch.EndWorkTime, intervalStart, intervalEnd);
            employee.SetPassword(request.Password);
        }
        catch (Exception exception) when (exception is Domain.Core.Exceptions.DomainException)
        {
            return Results.BadRequest(new { Message = exception.Message });
        }

        await db.Employees.AddAsync(employee, cancellationToken);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Results.Conflict(new { Message = "Não foi possível associar a conta à empresa." });
        }

        var (accessToken, refreshToken, expiresAt) = await CreateSessionAsync(employee, branch, db, jwtTokenService, configuration, cancellationToken);
        return Results.Created("/api/auth/login", new RegisterFromCompanyJoinLinkResponse(accessToken, refreshToken, expiresAt, employee.Id, branch.Id));
    }

    internal static async Task<(CompanyJoinLink Link, Branch Branch, TenantSubscription Subscription)?> ValidateAsync(
        string token,
        DottInContext db,
        ICompanyJoinLinkTokenService tokenService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || !tokenService.TryReadToken(token, out var linkId, out var payloadExpiry))
            return null;

        var link = await db.CompanyJoinLinks.SingleOrDefaultAsync(x => x.Id == linkId, cancellationToken);
        if (link is null || !link.IsActiveAt(DateTime.UtcNow) || link.ExpiresAt != payloadExpiry)
            return null;

        var branch = await db.Branches.SingleOrDefaultAsync(x => x.Id == link.BranchId, cancellationToken);
        if (branch?.IsActive != true || !branch.OwnerId.HasValue)
            return null;

        var subscription = await db.TenantSubscriptions.Include(x => x.Plan)
            .SingleOrDefaultAsync(x => x.OwnerId == branch.OwnerId.Value, cancellationToken);
        if (subscription?.Plan is null || !subscription.IsActiveOrTrialing)
            return null;

        return (link, branch, subscription);
    }

    internal static async Task<bool> HasSeatAvailableAsync(DottInContext db, Guid ownerId, TenantSubscription subscription, CancellationToken cancellationToken)
        => subscription.Plan!.HasUnlimitedEmployees ||
           await CountBillableEmployeesAsync(db, ownerId, cancellationToken) < subscription.Plan.MaxEmployees;

    private static Task<int> CountBillableEmployeesAsync(DottInContext db, Guid ownerId, CancellationToken cancellationToken)
        => db.Employees.CountAsync(employee => employee.IsActive && employee.Role != EmployeeRole.Owner &&
            db.Branches.Any(branch => branch.Id == employee.BranchId && branch.OwnerId == ownerId && branch.IsActive), cancellationToken);

    internal static (TimeOnly Start, TimeOnly End) DefaultInterval(Branch branch)
        => (branch.StartWorkTime.AddMinutes(15), branch.StartWorkTime.AddMinutes(30));

    private static async Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> CreateSessionAsync(
        Employee employee, Branch branch, DottInContext db, ITokenService tokenService, IConfiguration configuration, CancellationToken cancellationToken)
    {
        var jwt = configuration.GetSection("JwtSettings");
        var expiration = int.Parse(jwt["ExpirationMinutes"]!);
        var accessToken = tokenService.GenerateToken(employee.Id, branch.Id, branch.OwnerId ?? employee.Id,
            employee.Role.ToString(), jwt["SecretKey"]!, jwt["Issuer"]!, jwt["Audience"]!, expiration);
        var refreshToken = new RefreshToken(employee.Id, branch.Id);
        await db.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return (accessToken, refreshToken.PlainTextToken!, DateTime.UtcNow.AddMinutes(expiration));
    }
}
