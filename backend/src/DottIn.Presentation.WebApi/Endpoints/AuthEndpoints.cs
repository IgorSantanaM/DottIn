using System.Security.Claims;
using DottIn.Domain.Auth;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Employees;
using DottIn.Infra.Services.Auth;
using DottIn.Presentation.WebApi.DTOs.Auth;
using DottIn.Presentation.WebApi.Endpoints.Internal;
using Microsoft.AspNetCore.Mvc;

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
                .AllowAnonymous();

            group.MapPost("/login/pin", HandlePinLoginAsync)
                .WithName(nameof(HandlePinLoginAsync))
                .WithSummary("Login with PIN")
                .WithDescription("Authenticates an employee using CPF, PIN, and CompanyCode.")
                .Produces<LoginResponse>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status404NotFound)
                .AllowAnonymous();

            group.MapPost("/login/fingerprint", HandleFingerprintLoginAsync)
                .WithName(nameof(HandleFingerprintLoginAsync))
                .WithSummary("Login with Fingerprint")
                .WithDescription("Authenticates an employee using CPF, CompanyCode, and Fingerprint Token.")
                .Produces<LoginResponse>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status404NotFound)
                .AllowAnonymous();

            group.MapPost("/refresh", HandleRefreshTokenAsync)
                .WithName(nameof(HandleRefreshTokenAsync))
                .WithSummary("Refresh Access Token")
                .WithDescription("Issues a new access token and refresh token using a valid refresh token.")
                .Produces<RefreshTokenResponse>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .AllowAnonymous();

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
            CancellationToken cancellationToken)
        {
            var branch = await branchRepository.GetByCodeAsync(request.CompanyCode);
            if (branch == null)
                return Results.NotFound(new { Message = "Empresa não encontrada" });

            var employee = await employeeRepository.GetByCPFAsync(request.Cpf);
            if (employee == null)
                return Results.NotFound(new { Message = "Funcionário não encontrado" });

            var employeeBranch = await ValidateEmployeeBelongsToCompanyAsync(employee, branch, branchRepository, cancellationToken);
            if (employeeBranch == null)
                return Results.NotFound(new { Message = "Funcionário não pertence a esta empresa" });

            if (!employee.VerifyPassword(request.Password))
                return Results.Unauthorized();

            return await GenerateLoginResponseAsync(employeeBranch, employee, tokenService, refreshTokenRepository, unitOfWork, configuration, cancellationToken);
        }

        private static async Task<IResult> HandlePinLoginAsync(
            [FromBody] PinLoginRequest request,
            [FromServices] IBranchRepository branchRepository,
            [FromServices] IEmployeeRepository employeeRepository,
            [FromServices] ITokenService tokenService,
            [FromServices] IRefreshTokenRepository refreshTokenRepository,
            [FromServices] IUnitOfWork unitOfWork,
            [FromServices] IConfiguration configuration,
            CancellationToken cancellationToken)
        {
            var branch = await branchRepository.GetByCodeAsync(request.CompanyCode);
            if (branch == null)
                return Results.NotFound(new { Message = "Empresa não encontrada" });

            var employee = await employeeRepository.GetByCPFAsync(request.Cpf);
            if (employee == null)
                return Results.NotFound(new { Message = "Funcionário não encontrado" });

            var employeeBranch = await ValidateEmployeeBelongsToCompanyAsync(employee, branch, branchRepository, cancellationToken);
            if (employeeBranch == null)
                return Results.NotFound(new { Message = "Funcionário não pertence a esta empresa" });

            if (!employee.VerifyPin(request.Pin))
                return Results.Unauthorized();

            return await GenerateLoginResponseAsync(employeeBranch, employee, tokenService, refreshTokenRepository, unitOfWork, configuration, cancellationToken);
        }

        private static async Task<IResult> HandleFingerprintLoginAsync(
            [FromBody] FingerprintLoginRequest request,
            [FromServices] IBranchRepository branchRepository,
            [FromServices] IEmployeeRepository employeeRepository,
            [FromServices] ITokenService tokenService,
            [FromServices] IRefreshTokenRepository refreshTokenRepository,
            [FromServices] IUnitOfWork unitOfWork,
            [FromServices] IConfiguration configuration,
            CancellationToken cancellationToken)
        {
            var branch = await branchRepository.GetByCodeAsync(request.CompanyCode);
            if (branch == null)
                return Results.NotFound(new { Message = "Empresa não encontrada" });

            var employee = await employeeRepository.GetByCPFAsync(request.Cpf);
            if (employee == null)
                return Results.NotFound(new { Message = "Funcionário não encontrado" });

            var employeeBranch = await ValidateEmployeeBelongsToCompanyAsync(employee, branch, branchRepository, cancellationToken);
            if (employeeBranch == null)
                return Results.NotFound(new { Message = "Funcionário não pertence a esta empresa" });

            if (!employee.VerifyFingerprint(request.FingerprintToken))
                return Results.Unauthorized();

            return await GenerateLoginResponseAsync(employeeBranch, employee, tokenService, refreshTokenRepository, unitOfWork, configuration, cancellationToken);
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
            CancellationToken cancellationToken)
        {
            var existingToken = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

            if (existingToken == null || !existingToken.IsActive)
                return Results.Unauthorized();

            var employee = await employeeRepository.GetByIdAsync(existingToken.EmployeeId, cancellationToken);
            if (employee == null || !employee.IsActive)
                return Results.Unauthorized();

            var branch = await branchRepository.GetByIdAsync(existingToken.BranchId, cancellationToken);
            if (branch == null || !branch.IsActive)
                return Results.Unauthorized();

            await refreshTokenRepository.DeleteAsync(existingToken);

            var jwtSettings = configuration.GetSection("JwtSettings");
            var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"]!);

            var accessToken = tokenService.GenerateToken(
                employee.Id,
                branch.Id,
                jwtSettings["SecretKey"]!,
                jwtSettings["Issuer"]!,
                jwtSettings["Audience"]!,
                expirationMinutes);

            var newRefreshToken = new RefreshToken(employee.Id, branch.Id);
            await refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Results.Ok(new RefreshTokenResponse(
                accessToken,
                newRefreshToken.Token,
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

        private static async Task<IResult> GenerateLoginResponseAsync(
            Branch branch,
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
                branch.Id,
                jwtSettings["SecretKey"]!,
                jwtSettings["Issuer"]!,
                jwtSettings["Audience"]!,
                expirationMinutes);

            var refreshToken = new RefreshToken(employee.Id, branch.Id);
            await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var isOwner = branch.OwnerId.HasValue && branch.OwnerId.Value == employee.Id;

            var response = new LoginResponse(
                AccessToken: accessToken,
                RefreshToken: refreshToken.Token,
                ExpiresAt: DateTime.UtcNow.AddMinutes(expirationMinutes),
                Employee: new EmployeeInfoDto(employee.Id, employee.Name, employee.CPF.Value, employee.ImageUrl),
                BranchId: branch.Id,
                IsOwner: isOwner,
                IsHeadquarters: branch.IsHeadquarters
            );

            return Results.Ok(response);
        }
    }
}
