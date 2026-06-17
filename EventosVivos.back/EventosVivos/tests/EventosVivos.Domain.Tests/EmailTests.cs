using EventosVivos.Domain.Exceptions;
using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Domain.Tests;

public sealed class EmailTests
{
    [Theory]
    [InlineData("usuario@example.com")]
    [InlineData("nombre.apellido@dominio.co")]
    public void TryCreate_WithValidEmail_ReturnsTrue(string validEmail)
    {
        var wasCreated = Email.TryCreate(value: validEmail, email: out var email);

        Assert.True(wasCreated);
        Assert.Equal(expected: validEmail, actual: email?.Value);
    }

    [Theory]
    [InlineData("correo-invalido")]
    [InlineData("@dominio.com")]
    [InlineData("usuario@")]
    [InlineData("")]
    public void TryCreate_WithInvalidEmail_ReturnsFalse(string invalidEmail)
    {
        var wasCreated = Email.TryCreate(value: invalidEmail, email: out var email);

        Assert.False(wasCreated);
        Assert.Null(email);
    }

    [Fact]
    public void Create_WithInvalidEmail_ThrowsInvalidEmailFormatException()
    {
        Assert.Throws<InvalidEmailFormatException>(testCode: () => Email.Create(value: "correo-invalido"));
    }

    [Fact]
    public void Equals_IsCaseInsensitive()
    {
        var first = Email.Create(value: "Usuario@Example.com");
        var second = Email.Create(value: "usuario@example.com");

        Assert.Equal(expected: first, actual: second);
    }
}
