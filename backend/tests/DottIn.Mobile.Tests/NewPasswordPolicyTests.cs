using DottIn.Mobile.Services;

namespace DottIn.Mobile.Tests;

public sealed class NewPasswordPolicyTests
{
    [Fact]
    public void StrongDifferentConfirmedPassword_IsAccepted()
        => Assert.True(NewPasswordPolicy.IsValid("SenhaAtual1!", "NovaSenha123!", "NovaSenha123!"));

    [Theory]
    [InlineData("curta1!A")]
    [InlineData("semmaiuscula1!")]
    [InlineData("SEMMINUSCULA1!")]
    [InlineData("SemNumeroAqui!")]
    [InlineData("SemSimbolo123")]
    public void WeakPassword_IsRejected(string candidate)
        => Assert.False(NewPasswordPolicy.IsValid("SenhaAtual1!", candidate, candidate));

    [Fact]
    public void SameOrUnconfirmedPassword_IsRejected()
    {
        Assert.False(NewPasswordPolicy.IsValid("SenhaAtual1!", "SenhaAtual1!", "SenhaAtual1!"));
        Assert.False(NewPasswordPolicy.IsValid("SenhaAtual1!", "NovaSenha123!", "OutraSenha123!"));
    }
}
