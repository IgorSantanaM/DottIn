using System.Security.Claims;
using DottIn.Application.Features.Auth.DTOs;
using DottIn.Application.Features.Employees.Commands.RegisterOwner;
using DottIn.Application.Features.Subscriptions.Services;
using DottIn.Application.Shared.DTOS;
using DottIn.Domain.Auth;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Employees;
using DottIn.Infra.Services.Auth;
using DottIn.Presentation.WebApi.DTOs.Auth;
using DottIn.Presentation.WebApi.Endpoints.Internal;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Presentation.WebApi.Endpoints
{
    public class AuthEndpoints : IEndpoint
    {
        private const string Tag = "Auth";

        public static void DefineEndpoints(WebApplication app)
        {
            var group = app.MapGroup("/api/auth")
                .WithTags(Tag)
                .RequireAuthorization();

            group.MapPost("/login", HandleLoginAsync)
                .WithName(nameof(HandleLoginAsync))
                .WithSummary("Login with Password")
                .WithDescription("Authenticates an employee using CPF, Password, and CompanyCode.")
                .Produces<LoginResponse>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status404NotFound)
                .AllowAnonymous()
                .RequireRateLimiting("public-auth");

            group.MapPost("/login/pin", HandlePinLoginAsync)
                .WithName(nameof(HandlePinLoginAsync))
                .WithSummary("Login with PIN")
                .WithDescription("Authenticates an employee using CPF, PIN, and CompanyCode.")
                .Produces<LoginResponse>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status404NotFound)
                .AllowAnonymous()
                .RequireRateLimiting("public-auth");

            group.MapPost("/login/fingerprint", HandleFingerprintLoginAsync)
                .WithName(nameof(HandleFingerprintLoginAsync))
                .WithSummary("Login with Fingerprint")
                .WithDescription("Authenticates an employee using CPF, CompanyCode, and Fingerprint Token.")
                .Produces<LoginResponse>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status404NotFound)
                .AllowAnonymous()
                .RequireRateLimiting("public-auth");

            group.MapPost("/refresh", HandleRefreshTokenAsync)
                .WithName(nameof(HandleRefreshTokenAsync))
                .WithSummary("Refresh Access Token")
                .WithDescription("Issues a new access token and refresh token using a valid refresh token.")
                .Produces<RefreshTokenResponse>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .AllowAnonymous()
                .RequireRateLimiting("public-auth");

            group.MapPost("/register/owner", HandleRegisterOwnerAsync)
                .WithName(nameof(HandleRegisterOwnerAsync))
                .WithSummary("Register Owner Account")
                .WithDescription("Creates a new owner account (no branch or schedule). Returns tokens for auto-login.")
                .Produces<RegisterOwnerResponse>(StatusCodes.Status201Created)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status409Conflict)
                .AllowAnonymous()
                .RequireRateLimiting("public-auth");

            group.MapPost("/logout", HandleLogoutAsync)
                .WithName(nameof(HandleLogoutAsync))
                .WithSummary("Logout")
                .WithDescription("Revokes all refresh tokens for the authenticated employee.")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status401Unauthorized);

            group.MapPost("/register-fingerprint", HandleRegisterFingerprintAsync)
                .WithName(nameof(HandleRegisterFingerprintAsync))
                .WithSummary("Register Fingerprint Token")
                .WithDescription("Registers a device's fingerprint token using password authentication.")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status404NotFound);

            group.MapPut("/change-password", HandleChangePasswordAsync)
                .WithName(nameof(HandleChangePasswordAsync))
                .WithSummary("Change Password")
                .WithDescription("Changes employee password after verifying current password.")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status400BadRequest);

            group.MapPut("/change-pin", HandleChangePinAsync)
                .WithName(nameof(HandleChangePinAsync))
                .WithSummary("Change PIN")
                .WithDescription("Changes or sets employee PIN after verifying current password.")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status400BadRequest);
        }

        private static async Task<Branch?> ValidateEmployeeBelongsToCompanyAsync(
            Employee employee,
            Branch companyBranch,
            IBranchRepository branchRepository,
            CancellationToken cancellationToken)
        {
            if (employee.BranchId == companyBranch.Id)
                return companyBranch;

            var employeeBranch = await branchRepository.GetByIdAsync(employee.BranchId, cancellationToken);
            if (employeeBranch == null)
                return null;

            if (companyBranch.OwnerId.HasValue && employeeBranch.OwnerId.HasValue &&
                companyBranch.OwnerId.Value == employeeBranch.OwnerId.Value)
                return employeeBranch;

            return null;
        }

        #region Login Handlers

        private static async Task<IResult> HandleLoginAsync(
            [FromBody] LoginRequest request,
            [FromServices] IBranchRepository branchRepository,
            [FromServices] IEmployeeRepository employeeRepository,
            [FromServices] ITokenService tokenService,
            [FromServices] IRefreshTokenRepository refreshTokenRepository,
            [FromServices] IUnitOfWork unitOfWork,
            [FromServices] IConfiguration configuration,
            [FromServices] ITenantSubscriptionService subscriptionService,
            [FromServices] IValidator<LoginRequest> validator,
            CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request);

            var employee = await employeeRepository.GetByCPFAsync(request.Cpf, cancellationToken);
            if (employee == null)
                return Results.NotFound(new { Message = "Funcionário não encontrado" });

            if (!employee.IsActive)
                return Results.Unauthorized();

            if (!employee.VerifyPassword(request.Password))
                return Results.Unauthorized();

            if (employee.BranchId == Guid.Empty && employee.Role == EmployeeRole.Owner)
                return await GenerateUnassignedOwnerLoginResponseAsync(employee, tokenService, refreshTokenRepository, unitOfWork, configuration, cancellationToken);

            var branch = await branchRepository.GetByIdAsync(employee.BranchId, cancellationToken);
            if (branch == null)
                return Results.NotFound(new { Message = "Filial não encontrada" });

            if (!branch.IsActive)
                return Results.Unauthorized();

            return await GenerateLoginResponseAsync(branch, employee, tokenService, refreshTokenRepository, unitOfWork, configuration, subscriptionService, cancellationToken);
        }

        private static async Task<IResult> HandlePinLoginAsync(
            [FromBody] PinLoginRequest request,
            [FromServices] IBranchRepository branchRepository,
            [FromServices] IEmployeeRepository employeeRepository,
            [FromServices] ITokenService tokenService,
            [FromServices] IRefreshTokenRepository refreshTokenRepository,
            [FromServices] IUnitOfWork unitOfWork,
            [FromServices] IConfiguration configuration,
            [FromServices] ITenantSubscriptionService subscriptionService,
            CancellationToken cancellationToken)
        {
            var branch = await branchRepository.GetByCodeAsync(request.CompanyCode);
            if (branch == null)
                return Results.NotFound(new { Message = "Empresa não encontrada" });

            var employee = await employeeRepository.GetByTenantAndCPFAsync(
                branch.OwnerId ?? branch.Id, request.Cpf, cancellationToken);
            if (employee == null)
                return Results.NotFound(new { Message = "Funcionário não encontrado" });

            if (!branch.IsActive || !employee.IsActive)
                return Results.Unauthorized();

            var employeeBranch = await ValidateEmployeeBelongsToCompanyAsync(employee, branch, branchRepository, cancellationToken);
            if (employeeBranch == null)
                return Results.NotFound(new { Message = "Funcionário não pertence a esta empresa" });

            if (!employee.VerifyPin(request.Pin))
                return Results.Unauthorized();

            return await GenerateLoginResponseAsync(employeeBranch, employee, tokenService, refreshTokenRepository, unitOfWork, configuration, subscriptionService, cancellationToken);
        }

        private static async Task<IResult> HandleFingerprintLoginAsync(
            [FromBody] FingerprintLoginRequest request,
            [FromServices] IBranchRepository branchRepository,
            [FromServices] IEmployeeRepository employeeRepository,
            [FromServices] ITokenService tokenService,
            [FromServices] IRefreshTokenRepository refreshTokenRepository,
            [FromServices] IUnitOfWork unitOfWork,
            [FromServices] IConfiguration configuration,
            [FromServices] ITenantSubscriptionService subscriptionService,
            CancellationToken cancellationToken)
        {
            var branch = await branchRepository.GetByCodeAsync(request.CompanyCode);
            if (branch == null)
                return Results.NotFound(new { Message = "Empresa não encontrada" });

            var employee = await employeeRepository.GetByTenantAndCPFAsync(
                branch.OwnerId ?? branch.Id, request.Cpf, cancellationToken);
            if (employee == null)
                return Results.NotFound(new { Message = "Funcionário não encontrado" });

            if (!branch.IsActive || !employee.IsActive)
                return Results.Unauthorized();

            var employeeBranch = await ValidateEmployeeBelongsToCompanyAsync(employee, branch, branchRepository, cancellationToken);
            if (employeeBranch == null)
                return Results.NotFound(new { Message = "Funcionário não pertence a esta empresa" });

            if (!employee.VerifyFingerprint(request.FingerprintToken))
                return Results.Unauthorized();

            return await GenerateLoginResponseAsync(employeeBranch, employee, tokenService, refreshTokenRepository, unitOfWork, configuration, subscriptionService, cancellationToken);
        }

        #endregion

        #region Token Management

        private static async Task<IResult> HandleRefreshTokenAsync(
            [FromBody] RefreshTokenRequest request,
            [FromServices] IRefreshTokenRepository refreshTokenRepository,
            [FromServices] IBranchRepository branchRepository,
            [FromServices] IEmployeeRepository employeeRepository,
            [FromServices] ITokenService tokenService,
            [FromServices] IUnitOfWork unitOfWork,
            [FromServices] IConfiguration configuration,
            [FromServices] ITenantSubscriptionService subscriptionService,
            CancellationToken cancellationToken)
        {
            var existingToken = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

            if (existingToken == null || !existingToken.IsActive)
                return Results.Unauthorized();

            var employee = await employeeRepository.GetByIdAsync(existingToken.EmployeeId, cancellationToken);
            if (employee == null || !employee.IsActive)
                return Results.Unauthorized();

            Branch? branch;
            if (existingToken.BranchId == Guid.Empty && employee.Role == EmployeeRole.Owner)
            {
                var ownerBranches = await branchRepository.GetByOwnerIdAsync(employee.Id, cancellationToken);
                branch = ownerBranches.FirstOrDefault(x => x.IsHeadquarters) ?? ownerBranches.FirstOrDefault();
            }
            else
            {
                branch = await branchRepository.GetByIdAsync(existingToken.BranchId, cancellationToken);
            }

            if (branch == null || !branch.IsActive)
                return Results.Unauthorized();

            await refreshTokenRepository.DeleteAsync(existingToken);

            var jwtSettings = configuration.GetSection("JwtSettings");
            var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"]!);

            var accessToken = tokenService.GenerateToken(
                employee.Id,
                branch.Id,
                branch.OwnerId ?? employee.Id,
                employee.Role.ToString(),
                jwtSettings["SecretKey"]!,
                jwtSettings["Issuer"]!,
                jwtSettings["Audience"]!,
                expirationMinutes);

            var newRefreshToken = new RefreshToken(employee.Id, branch.Id);
            await refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new RefreshTokenResponse(
                accessToken,
                newRefreshToken.PlainTextToken!,
                DateTime.UtcNow.AddMinutes(expirationMinutes)));
        }

        private static async Task<IResult> HandleLogoutAsync(
            ClaimsPrincipal user,
            [FromServices] IRefreshTokenRepository refreshTokenRepository,
            [FromServices] IUnitOfWork unitOfWork,
            CancellationToken cancellationToken)
        {
            var employeeIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                                  ?? user.FindFirstValue("sub");

            if (string.IsNullOrEmpty(employeeIdClaim) || !Guid.TryParse(employeeIdClaim, out var employeeId))
                return Results.Unauthorized();

            await refreshTokenRepository.DeleteAllByEmployeeAsync(employeeId, cancellationToken);

            return Results.NoContent();
        }

        #endregion

        #region Authenticated Operations

        private static async Task<IResult> HandleRegisterFingerprintAsync(
            [FromBody] RegisterFingerprintRequest request,
            ClaimsPrincipal user,
            [FromServices] IBranchRepository branchRepository,
            [FromServices] IEmployeeRepository employeeRepository,
            [FromServices] IUnitOfWork unitOfWork,
            CancellationToken cancellationToken)
        {
            var branch = await branchRepository.GetByCodeAsync(request.CompanyCode);
            if (branch == null)
                return Results.NotFound(new { Message = "Empresa não encontrada" });

            var employee = await employeeRepository.GetByTenantAndCPFAsync(
                branch.OwnerId ?? branch.Id, request.Cpf, cancellationToken);
            if (employee == null)
                return Results.NotFound(new { Message = "Funcionário não encontrado" });

            if (!IsCurrentEmployee(user, employee.Id))
                return Results.Forbid();

            var employeeBranch = await ValidateEmployeeBelongsToCompanyAsync(employee, branch, branchRepository, cancellationToken);
            if (employeeBranch == null)
                return Results.NotFound(new { Message = "Funcionário não pertence a esta empresa" });

            if (!employee.VerifyPassword(request.Password))
                return Results.Unauthorized();

            employee.SetFingerprint(request.FingerprintToken);
            await employeeRepository.UpdateAsync(employee);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        }

        private static async Task<IResult> HandleChangePasswordAsync(
            [FromBody] ChangePasswordRequest request,
            ClaimsPrincipal user,
            [FromServices] IBranchRepository branchRepository,
            [FromServices] IEmployeeRepository employeeRepository,
            [FromServices] IUnitOfWork unitOfWork,
            CancellationToken cancellationToken)
        {
            var branch = await branchRepository.GetByCodeAsync(request.CompanyCode);
            if (branch == null)
                return Results.NotFound(new { Message = "Empresa não encontrada" });

            var employee = await employeeRepository.GetByCPFAsync(request.Cpf);
            if (employee == null)
                return Results.NotFound(new { Message = "Funcionário não encontrado" });

            if (!IsCurrentEmployee(user, employee.Id))
                return Results.Forbid();

            var employeeBranch = await ValidateEmployeeBelongsToCompanyAsync(employee, branch, branchRepository, cancellationToken);
            if (employeeBranch == null)
                return Results.NotFound(new { Message = "Funcionário não pertence a esta empresa" });

            if (!employee.VerifyPassword(request.CurrentPassword))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
                return Results.BadRequest(new { Message = "A nova senha deve ter no mínimo 6 caracteres." });

            if (request.CurrentPassword == request.NewPassword)
                return Results.BadRequest(new { Message = "A nova senha deve ser diferente da atual." });

            employee.SetPassword(request.NewPassword);
            await employeeRepository.UpdateAsync(employee);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        }

        private static async Task<IResult> HandleChangePinAsync(
            [FromBody] ChangePinRequest request,
            ClaimsPrincipal user,
            [FromServices] IBranchRepository branchRepository,
            [FromServices] IEmployeeRepository employeeRepository,
            [FromServices] IUnitOfWork unitOfWork,
            CancellationToken cancellationToken)
        {
            var branch = await branchRepository.GetByCodeAsync(request.CompanyCode);
            if (branch == null)
                return Results.NotFound(new { Message = "Empresa não encontrada" });

            var employee = await employeeRepository.GetByCPFAsync(request.Cpf);
            if (employee == null)
                return Results.NotFound(new { Message = "Funcionário não encontrado" });

            if (!IsCurrentEmployee(user, employee.Id))
                return Results.Forbid();

            var employeeBranch = await ValidateEmployeeBelongsToCompanyAsync(employee, branch, branchRepository, cancellationToken);
            if (employeeBranch == null)
                return Results.NotFound(new { Message = "Funcionário não pertence a esta empresa" });

            if (!employee.VerifyPassword(request.CurrentPassword))
                return Results.Unauthorized();

            employee.SetPin(request.NewPin);
            await employeeRepository.UpdateAsync(employee);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        }

        #endregion

        #region Owner Registration

        private static async Task<IResult> HandleRegisterOwnerAsync(
            [FromBody] RegisterOwnerRequest request,
            [FromServices] IMediator mediator,
            [FromServices] IEmployeeRepository employeeRepository,
            [FromServices] ITokenService tokenService,
            [FromServices] IRefreshTokenRepository refreshTokenRepository,
            [FromServices] IUnitOfWork unitOfWork,
            [FromServices] IConfiguration configuration,
            [FromServices] IValidator<RegisterOwnerRequest> validator,
            CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request);

            var documentType = Enum.TryParse<Domain.ValueObjects.DocumentType>(request.Document.Type, true, out var dt)
                ? dt
                : Domain.ValueObjects.DocumentType.CPF;

            var command = new RegisterOwnerCommand(
                request.Name,
                new DocumentDto(request.Document.Value, documentType),
                request.Password);

            var employeeId = await mediator.Send(command, cancellationToken);

            // Auto-login: generate tokens for the new owner
            var employee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
            if (employee is null)
                return Results.BadRequest(new { Message = "Erro interno ao criar a conta." });

            var jwtSettings = configuration.GetSection("JwtSettings");
            var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"]!);

            var accessToken = tokenService.GenerateToken(
                employee.Id,
                Guid.Empty, // Owner has no branch yet
                employee.Id,
                employee.Role.ToString(),
                jwtSettings["SecretKey"]!,
                jwtSettings["Issuer"]!,
                jwtSettings["Audience"]!,
                expirationMinutes);

            var refreshToken = new Domain.Auth.RefreshToken(employee.Id, Guid.Empty);
            await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new RegisterOwnerResponse(
                AccessToken: accessToken,
                RefreshToken: refreshToken.PlainTextToken!,
                ExpiresAt: DateTime.UtcNow.AddMinutes(expirationMinutes),
                Employee: new EmployeeInfoDto(employee.Id, employee.Name, employee.CPF.Value, employee.ImageUrl));

            return Results.Created($"/api/auth/register/owner/{employeeId}", response);
        }

        #endregion

        private static async Task<IResult> GenerateUnassignedOwnerLoginResponseAsync(
            Employee employee,
            ITokenService tokenService,
            IRefreshTokenRepository refreshTokenRepository,
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            CancellationToken cancellationToken)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"]!);

            var accessToken = tokenService.GenerateToken(
                employee.Id,
                Guid.Empty,
                employee.Id,
                employee.Role.ToString(),
                jwtSettings["SecretKey"]!,
                jwtSettings["Issuer"]!,
                jwtSettings["Audience"]!,
                expirationMinutes);

            var refreshToken = new RefreshToken(employee.Id, Guid.Empty);
            await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new LoginResponse(
                AccessToken: accessToken,
                RefreshToken: refreshToken.PlainTextToken!,
                ExpiresAt: DateTime.UtcNow.AddMinutes(expirationMinutes),
                Employee: new EmployeeInfoDto(employee.Id, employee.Name, employee.CPF.Value, employee.ImageUrl),
                BranchId: Guid.Empty,
                IsOwner: true,
                IsHeadquarters: false,
                Subscription: null,
                CompanyCode: string.Empty);

            return Results.Ok(response);
        }

        private static async Task<IResult> GenerateLoginResponseAsync(
            Branch branch,
            Employee employee,
            ITokenService tokenService,
            IRefreshTokenRepository refreshTokenRepository,
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            ITenantSubscriptionService subscriptionService,
            CancellationToken cancellationToken)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"]!);

            var accessToken = tokenService.GenerateToken(
                employee.Id,
                branch.Id,
                branch.OwnerId ?? employee.Id,
                employee.Role.ToString(),
                jwtSettings["SecretKey"]!,
                jwtSettings["Issuer"]!,
                jwtSettings["Audience"]!,
                expirationMinutes);

            var refreshToken = new RefreshToken(employee.Id, branch.Id);
            await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var isOwner = employee.Role == EmployeeRole.Owner;

            SubscriptionInfoDto? subscriptionInfo = null;
            if (branch.OwnerId.HasValue)
            {
                var subscription = await subscriptionService.GetByOwnerIdAsync(branch.OwnerId.Value, cancellationToken);
                if (subscription != null)
                {
                    subscriptionInfo = new SubscriptionInfoDto(
                        PlanName: subscription.PlanName,
                        MaxEmployees: subscription.MaxEmployees,
                        MaxBranches: subscription.MaxBranches,
                        CanAddEmployee: subscription.CanAddEmployee,
                        CanAddBranch: subscription.CanAddBranch
                    );
                }
            }

            var response = new LoginResponse(
                AccessToken: accessToken,
                RefreshToken: refreshToken.PlainTextToken!,
                ExpiresAt: DateTime.UtcNow.AddMinutes(expirationMinutes),
                Employee: new EmployeeInfoDto(employee.Id, employee.Name, employee.CPF.Value, employee.ImageUrl),
                BranchId: branch.Id,
                IsOwner: isOwner,
                IsHeadquarters: branch.IsHeadquarters,
                Subscription: subscriptionInfo,
                CompanyCode: branch.CompanyCode
            );

            return Results.Ok(response);
        }

        private static bool IsCurrentEmployee(ClaimsPrincipal user, Guid employeeId)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            return Guid.TryParse(value, out var authenticatedId) && authenticatedId == employeeId;
        }
    }
}
