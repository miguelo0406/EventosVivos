using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Domain.Tests;

// Pruebas del agregado Venue (CRUD de organizador, valor agregado): validan las invariantes
// propias del venue. Los guardas cruzados con Event (no borrar referenciado, no bajar el aforo
// por debajo de un evento) se prueban a nivel de servicio/integración, no aquí.
public sealed class VenueTests
{
    [Fact]
    public void Create_WithValidData_ReturnsVenue()
    {
        var venue = Venue.Create(id: 4, name: "Teatro Colón", capacity: 300, city: "Bogotá");

        Assert.Equal(expected: 4, actual: venue.Id);
        Assert.Equal(expected: "Teatro Colón", actual: venue.Name);
        Assert.Equal(expected: 300, actual: venue.Capacity);
        Assert.Equal(expected: "Bogotá", actual: venue.City);
    }

    [Fact]
    public void Create_TrimsNameAndCity()
    {
        var venue = Venue.Create(id: 5, name: "  Sala Beta  ", capacity: 80, city: "  Cali  ");

        Assert.Equal(expected: "Sala Beta", actual: venue.Name);
        Assert.Equal(expected: "Cali", actual: venue.City);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ThrowsDomainValidationException(string invalidName)
    {
        Assert.Throws<DomainValidationException>(testCode: () =>
            Venue.Create(id: 6, name: invalidName, capacity: 100, city: "Bogotá"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Create_WithNonPositiveCapacity_ThrowsDomainValidationException(int invalidCapacity)
    {
        Assert.Throws<DomainValidationException>(testCode: () =>
            Venue.Create(id: 7, name: "Sala Gamma", capacity: invalidCapacity, city: "Bogotá"));
    }

    [Fact]
    public void Create_WithEmptyCity_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(testCode: () =>
            Venue.Create(id: 8, name: "Sala Delta", capacity: 100, city: "   "));
    }

    [Fact]
    public void Update_WithValidData_ChangesEditableFields()
    {
        var venue = Venue.Create(id: 9, name: "Sala Norte", capacity: 50, city: "Bogotá");

        venue.Update(name: "Sala Norte Renovada", capacity: 120, city: "Medellín");

        Assert.Equal(expected: "Sala Norte Renovada", actual: venue.Name);
        Assert.Equal(expected: 120, actual: venue.Capacity);
        Assert.Equal(expected: "Medellín", actual: venue.City);
    }

    [Fact]
    public void Update_WithInvalidData_ThrowsAndDoesNotMutate()
    {
        var venue = Venue.Create(id: 10, name: "Sala Épsilon", capacity: 60, city: "Bogotá");

        Assert.Throws<DomainValidationException>(testCode: () =>
            venue.Update(name: "x", capacity: 0, city: ""));

        // La entidad conserva su estado original tras un Update inválido (validación antes de mutar).
        Assert.Equal(expected: "Sala Épsilon", actual: venue.Name);
        Assert.Equal(expected: 60, actual: venue.Capacity);
    }
}
