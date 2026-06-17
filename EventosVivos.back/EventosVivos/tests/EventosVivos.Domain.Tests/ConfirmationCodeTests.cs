using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Domain.Tests;

public sealed class ConfirmationCodeTests
{
    [Fact]
    public void Generate_ReturnsValueMatchingExpectedFormat()
    {
        var code = ConfirmationCode.Generate();

        Assert.Matches(expectedRegexPattern: "^EV-\\d{6}$", actualString: code.Value);
    }

    [Fact]
    public void FromExisting_RoundTripsTheSameValue()
    {
        var code = ConfirmationCode.FromExisting(value: "EV-123456");

        Assert.Equal(expected: "EV-123456", actual: code.Value);
    }
}
