using DottIn.Application.Features.Auth.DTOs;
using DottIn.Application.Features.Auth.Validators;

namespace DottIn.WebApi.IntegrationTests.Auth;

public sealed class LoginRequestValidatorTests
{
    [Fact]
    public void DoesNotExposeACompanyCodeInThePasswordLoginContract()
    {
        Assert.Null(typeof(LoginRequest).GetProperty("CompanyCode"));
        Assert.Null(typeof(DottIn.Admin.Models.LoginRequest).GetProperty("CompanyCode"));
    }

    [Fact]
    public async Task AcceptsCpfAndPasswordWithoutACompanyCode()
    {
        var validator = new LoginRequestValidator();

        var result = await validator.ValidateAsync(new LoginRequest(
            Cpf: "12345678901",
            Password: "Password1!"),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsValid);
    }
}
