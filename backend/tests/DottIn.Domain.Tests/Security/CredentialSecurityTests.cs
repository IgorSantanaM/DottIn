using DottIn.Domain.Auth;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;
using DottIn.Domain.ValueObjects;

namespace DottIn.Domain.Tests.Security;

public sealed class CredentialSecurityTests
{
    [Fact]
    public void RefreshToken_StoresOnlyHashAndReturnsPlainTextOnce()
    {
        var token = new RefreshToken(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(string.IsNullOrWhiteSpace(token.PlainTextToken));
        Assert.NotEqual(token.PlainTextToken, token.Token);
        Assert.Equal(RefreshToken.HashToken(token.PlainTextToken!), token.Token);
        Assert.Equal(64, token.Token.Length);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("alllowercase1!")]
    [InlineData("ALLUPPERCASE1!")]
    [InlineData("NoNumberHere!")]
    [InlineData("NoSymbolHere1")]
    public void Employee_RejectsWeakPasswords(string password)
    {
        var document = new Document("52998224725");

        Assert.Throws<DomainException>(() => new Employee("Pessoa Teste", document, password));
    }

    [Fact]
    public void Employee_AcceptsStrongPassword()
    {
        var employee = new Employee(
            "Pessoa Teste",
            new Document("52998224725"),
            "SenhaForte1!");

        Assert.True(employee.VerifyPassword("SenhaForte1!"));
    }
}
