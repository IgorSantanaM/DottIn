namespace DottIn.Application.Features.Auth.DTOs
{
    public record LoginRequest(string Cpf, string Password, string CompanyCode);
}
