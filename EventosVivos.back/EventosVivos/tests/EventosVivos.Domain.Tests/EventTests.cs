using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Domain.Tests;

public sealed class EventTests
{
    private static readonly DateTime CurrentTime = new(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc);

    private static Event CreateValidEvent(
        DateTime? startDateTime = null,
        DateTime? endDateTime = null,
        int maxCapacity = 100,
        int venueCapacity = 200,
        decimal ticketPrice = 50m)
    {
        var start = startDateTime ?? CurrentTime.AddDays(10);
        var end = endDateTime ?? start.AddHours(3);

        return Event.Create(
            title: "Conferencia de Tecnología",
            description: "Una descripción suficientemente larga para pasar la validación.",
            venueId: 1,
            venueCapacity: venueCapacity,
            maxCapacity: maxCapacity,
            startDateTime: start,
            endDateTime: end,
            ticketPrice: ticketPrice,
            type: EventType.Conference,
            currentTime: CurrentTime);
    }

    private static DateTime NextOccurrenceOf(DayOfWeek dayOfWeek, int hour, int minute)
    {
        var daysUntil = ((int)dayOfWeek - (int)CurrentTime.DayOfWeek + 7) % 7;
        daysUntil = daysUntil == 0 ? 7 : daysUntil;

        return CurrentTime.Date.AddDays(daysUntil).AddHours(hour).AddMinutes(minute);
    }

    [Fact]
    public void Create_WithValidData_ReturnsActiveEvent()
    {
        var createdEvent = CreateValidEvent();

        Assert.Equal(expected: EventStatus.Active, actual: createdEvent.Status);
        Assert.NotEqual(expected: Guid.Empty, actual: createdEvent.Id);
    }

    [Theory]
    [InlineData("abcd")]
    [InlineData("")]
    public void Create_WithInvalidTitleLength_ThrowsDomainValidationException(string invalidTitle)
    {
        var exception = Assert.Throws<DomainValidationException>(testCode: () => Event.Create(
            title: invalidTitle,
            description: "Una descripción suficientemente larga para pasar la validación.",
            venueId: 1,
            venueCapacity: 200,
            maxCapacity: 100,
            startDateTime: CurrentTime.AddDays(10),
            endDateTime: CurrentTime.AddDays(10).AddHours(3),
            ticketPrice: 50m,
            type: EventType.Conference,
            currentTime: CurrentTime));

        Assert.Contains(collection: exception.Errors, filter: error => error.Contains("título"));
    }

    [Fact]
    public void Create_WithMaxCapacityGreaterThanVenueCapacity_ThrowsVenueCapacityExceededException()
    {
        Assert.Throws<VenueCapacityExceededException>(testCode: () =>
            CreateValidEvent(maxCapacity: 300, venueCapacity: 200));
    }

    [Fact]
    public void Create_WithEndDateBeforeStartDate_ThrowsDomainValidationException()
    {
        var start = CurrentTime.AddDays(10);

        Assert.Throws<DomainValidationException>(testCode: () =>
            CreateValidEvent(startDateTime: start, endDateTime: start.AddHours(-1)));
    }

    [Fact]
    public void Create_WithPastStartDate_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(testCode: () =>
            CreateValidEvent(startDateTime: CurrentTime.AddDays(-1), endDateTime: CurrentTime.AddDays(1)));
    }

    // RN-03
    [Fact]
    public void Create_OnSaturdayAfter10Pm_ThrowsWeekendNightRestrictionException()
    {
        var saturdayNight = NextOccurrenceOf(dayOfWeek: DayOfWeek.Saturday, hour: 22, minute: 30);

        Assert.Throws<WeekendNightRestrictionException>(testCode: () =>
            CreateValidEvent(startDateTime: saturdayNight, endDateTime: saturdayNight.AddHours(2)));
    }

    [Fact]
    public void Create_OnSundayExactlyAt10Pm_ThrowsWeekendNightRestrictionException()
    {
        var sundayNight = NextOccurrenceOf(dayOfWeek: DayOfWeek.Sunday, hour: 22, minute: 0);

        Assert.Throws<WeekendNightRestrictionException>(testCode: () =>
            CreateValidEvent(startDateTime: sundayNight, endDateTime: sundayNight.AddHours(2)));
    }

    [Fact]
    public void Create_OnSaturdayBefore10Pm_DoesNotThrow()
    {
        var saturdayEvening = NextOccurrenceOf(dayOfWeek: DayOfWeek.Saturday, hour: 20, minute: 0);

        var createdEvent = CreateValidEvent(startDateTime: saturdayEvening, endDateTime: saturdayEvening.AddHours(2));

        Assert.Equal(expected: DayOfWeek.Saturday, actual: createdEvent.StartDateTime.DayOfWeek);
    }

    [Fact]
    public void Create_OnWeekdayAfter10Pm_DoesNotThrow()
    {
        var weekdayNight = NextOccurrenceOf(dayOfWeek: DayOfWeek.Wednesday, hour: 23, minute: 0);

        var createdEvent = CreateValidEvent(startDateTime: weekdayNight, endDateTime: weekdayNight.AddHours(2));

        Assert.Equal(expected: EventStatus.Active, actual: createdEvent.Status);
    }

    // RN-06
    [Fact]
    public void GetEffectiveStatus_AfterEndDateTime_ReturnsCompleted()
    {
        var start = CurrentTime.AddHours(2);
        var end = CurrentTime.AddHours(4);
        var createdEvent = CreateValidEvent(startDateTime: start, endDateTime: end);

        var effectiveStatus = createdEvent.GetEffectiveStatus(currentTime: end.AddMinutes(1));

        Assert.Equal(expected: EventStatus.Completed, actual: effectiveStatus);
    }

    [Fact]
    public void GetEffectiveStatus_BeforeEndDateTime_ReturnsActive()
    {
        var createdEvent = CreateValidEvent();

        var effectiveStatus = createdEvent.GetEffectiveStatus(currentTime: CurrentTime);

        Assert.Equal(expected: EventStatus.Active, actual: effectiveStatus);
    }

    [Fact]
    public void Cancel_WhenActive_SetsStatusToCancelled()
    {
        var createdEvent = CreateValidEvent();

        createdEvent.Cancel(currentTime: CurrentTime);

        Assert.Equal(expected: EventStatus.Cancelled, actual: createdEvent.Status);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ThrowsInvalidEventStateException()
    {
        var createdEvent = CreateValidEvent();
        createdEvent.Cancel(currentTime: CurrentTime);

        Assert.Throws<InvalidEventStateException>(testCode: () => createdEvent.Cancel(currentTime: CurrentTime));
    }

    [Fact]
    public void Cancel_WhenCompleted_ThrowsInvalidEventStateException()
    {
        var start = CurrentTime.AddHours(2);
        var end = CurrentTime.AddHours(4);
        var createdEvent = CreateValidEvent(startDateTime: start, endDateTime: end);

        Assert.Throws<InvalidEventStateException>(testCode: () => createdEvent.Cancel(currentTime: end.AddMinutes(1)));
    }
}
