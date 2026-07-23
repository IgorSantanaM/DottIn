namespace DottIn.Application.Features.Auth.DTOs
{
    public record RegisterOwnerRequest(string Name, RegisterOwnerDocumentDto Document, string Password);
}
