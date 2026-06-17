using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Services;

namespace EventosVivos.Domain.Tests;

public sealed class VenueScheduleConflictCheckerTests
{
    private static readonly DateTime CurrentTime = new(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc);

    private static Event CreateEvent(DateTime startDateTime, DateTime endDateTime)
    {
        return Event.Create(
            title: "Evento existente de prueba",
            description: "Descripción suficientemente larga para pasar la validación de dominio.",
            venueId: 1,
            venueCapacity: 200,
            maxCapacity: 100,
            startDateTime: startDateTime,
            endDateTime: endDateTime,
            ticketPrice: 50m,
            type: EventType.Conference,
            currentTime: CurrentTime);
    }

    [Fact]
    public void HasConflict_WithNoExistingEvents_ReturnsFalse()
    {
        var hasConflict = VenueScheduleConflictChecker.HasConflict(
            candidateStart: CurrentTime.AddDays(1),
            candidateEnd: CurrentTime.AddDays(1).AddHours(2),
            existingActiveEventsAtVenue: []);

        Assert.False(hasConflict);
    }

    [Fact]
    public void HasConflict_WithOverlappingInterval_ReturnsTrue()
    {
        var existingEvent = CreateEvent(
            startDateTime: CurrentTime.AddDays(1),
            endDateTime: CurrentTime.AddDays(1).AddHours(3));

        var hasConflict = VenueScheduleConflictChecker.HasConflict(
            candidateStart: CurrentTime.AddDays(1).AddHours(1),
            candidateEnd: CurrentTime.AddDays(1).AddHours(4),
            existingActiveEventsAtVenue: [existingEvent]);

        Assert.True(hasConflict);
    }

    [Fact]
    public void HasConflict_WithCandidateFullyInsideExistingInterval_ReturnsTrue()
    {
        var existingEvent = CreateEvent(
            startDateTime: CurrentTime.AddDays(1),
            endDateTime: CurrentTime.AddDays(1).AddHours(6));

        var hasConflict = VenueScheduleConflictChecker.HasConflict(
            candidateStart: CurrentTime.AddDays(1).AddHours(2),
            candidateEnd: CurrentTime.AddDays(1).AddHours(3),
            existingActiveEventsAtVenue: [existingEvent]);

        Assert.True(hasConflict);
    }

    [Fact]
    public void HasConflict_WithAdjacentNonOverlappingIntervals_ReturnsFalse()
    {
        var existingEvent = CreateEvent(
            startDateTime: CurrentTime.AddDays(1),
            endDateTime: CurrentTime.AddDays(1).AddHours(3));

        // El nuevo evento empieza justo cuando termina el existente: no se superponen.
        var hasConflict = VenueScheduleConflictChecker.HasConflict(
            candidateStart: CurrentTime.AddDays(1).AddHours(3),
            candidateEnd: CurrentTime.AddDays(1).AddHours(5),
            existingActiveEventsAtVenue: [existingEvent]);

        Assert.False(hasConflict);
    }

    [Fact]
    public void HasConflict_WithNonOverlappingInterval_ReturnsFalse()
    {
        var existingEvent = CreateEvent(
            startDateTime: CurrentTime.AddDays(1),
            endDateTime: CurrentTime.AddDays(1).AddHours(2));

        var hasConflict = VenueScheduleConflictChecker.HasConflict(
            candidateStart: CurrentTime.AddDays(2),
            candidateEnd: CurrentTime.AddDays(2).AddHours(2),
            existingActiveEventsAtVenue: [existingEvent]);

        Assert.False(hasConflict);
    }
}
