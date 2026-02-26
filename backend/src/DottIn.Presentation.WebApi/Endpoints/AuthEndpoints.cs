using DottIn.Domain.Branches;
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
                .WithTags(Tag);

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

            group.MapPost("/register-fingerprint", HandleRegisterFingerprintAsync)
                .WithName(nameof(HandleRegisterFingerprintAsync))
                .WithSummary("Register Fingerprint Token")
                .WithDescription("Registers a device's fingerprint token using password authentication.")
                .Produces(StatusCodes.Status204NoContent)
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
        }

        private static async Task<IResult> HandleLoginAsync(
            [FromBody] LoginRequest request,
            [FromServices] IBranchRepository branchRepository,
            [FromServices] IEmployeeRepository employeeRepository,
            [FromServices] ITokenService tokenService,
            [FromServices] IConfiguration configuration,
            CancellationToken cancellationToken)
        {
            var branch = await branchRepository.GetByCodeAsync(request.CompanyCode);
            if (branch == null)
                return Results.NotFound(new { Message = "Empresa não encontrada" });

            var employee = await employeeRepository.GetByCPFAsync(branch.Id, request.Cpf);
            if (employee == null)
                return Results.NotFound(new { Message = "Funcionário não encontrado" });

            if (!employee.VerifyPassword(request.Password))
                return Results.Unauthorized();

            return GenerateLoginResponse(branch, employee, tokenService, configuration);
        }

        private static async Task<IResult> HandlePinLoginAsync(
            [FromBody] PinLoginRequest request,
            [FromServices] IBranchRepository branchRepository,
            [FromServices] IEmployeeRepository employeeRepository,
            [FromServices] ITokenService tokenService,
            [FromServices] IConfiguration configuration,
            CancellationToken cancellationToken)
        {
            var branch = await branchRepository.GetByCodeAsync(request.CompanyCode);
            if (branch == null)
                return Results.NotFound(new { Message = "Empresa não encontrada" });

            var employee = await employeeRepository.GetByCPFAsync(branch.Id, request.Cpf);
            if (employee == null)
                return Results.NotFound(new { Message = "Funcionário não encontrado" });

            if (!employee.VerifyPin(request.Pin))
                return Results.Unauthorized();

            return GenerateLoginResponse(branch, employee, tokenService, configuration);
        }

        private static async Task<IResult> HandleRegisterFingerprintAsync(
            [FromBody] RegisterFingerprintRequest request,
            [FromServices] IBranchRepository branchRepository,
            [FromServices] IEmployeeRepository employeeRepository,
            CancellationToken cancellationToken)
        {
            var branch = await branchRepository.GetByCodeAsync(request.CompanyCode);
            if (branch == null)
                return Results.NotFound(new { Message = "Empresa não encontrada" });

            var employee = await employeeRepository.GetByCPFAsync(branch.Id, request.Cpf);
            if (employee == null)
                return Results.NotFound(new { Message = "Funcionário não encontrado" });

            if (!employee.VerifyPassword(request.Password))
                return Results.Unauthorized();

            employee.SetFingerprint(request.FingerprintToken);
            await employeeRepository.UpdateAsync(employee);

            return Results.NoContent();
        }

        private static async Task<IResult> HandleFingerprintLoginAsync(
            [FromBody] FingerprintLoginRequest request,
            [FromServices] IBranchRepository branchRepository,
            [FromServices] IEmployeeRepository employeeRepository,
            [FromServices] ITokenService tokenService,
            [FromServices] IConfiguration configuration,
            CancellationToken cancellationToken)
        {
            var branch = await branchRepository.GetByCodeAsync(request.CompanyCode);
            if (branch == null)
                return Results.NotFound(new { Message = "Empresa não encontrada" });

            var employee = await employeeRepository.GetByCPFAsync(branch.Id, request.Cpf);
            if (employee == null)
                return Results.NotFound(new { Message = "Funcionário não encontrado" });

            if (!employee.VerifyFingerprint(request.FingerprintToken))
                return Results.Unauthorized();

            return GenerateLoginResponse(branch, employee, tokenService, configuration);
        }

        private static IResult GenerateLoginResponse(
            Branch branch, 
            Employee employee, 
            ITokenService tokenService, 
            IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"]!);
            
            var token = tokenService.GenerateToken(
                employee.Id, 
                branch.Id, 
                jwtSettings["SecretKey"]!, 
                jwtSettings["Issuer"]!, 
                jwtSettings["Audience"]!, 
                expirationMinutes);

            var response = new LoginResponse(
                AccessToken: token,
                RefreshToken: "dummy-refresh-token",
                ExpiresAt: DateTime.UtcNow.AddMinutes(expirationMinutes),
                Employee: new EmployeeInfoDto(employee.Id, employee.Name, employee.ImageUrl),
                BranchId: branch.Id
            );

            return Results.Ok(response);
        }
    }
}
