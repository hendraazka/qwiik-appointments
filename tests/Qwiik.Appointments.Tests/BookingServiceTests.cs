using Moq;
using Qwiik.Appointments.Core;
using Qwiik.Appointments.Core.Entities;

public class BookingServiceTests
{
    private static readonly DateOnly Day1 = new(2099, 1, 5);
    private readonly Mock<IAppointmentRepository> _appts = new();
    private readonly Mock<IAgencyConfigRepository> _config = new();

    public BookingServiceTests() =>
        _config.Setup(c => c.GetMaxPerDayAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);

    private BookingService Sut() => new(_appts.Object, _config.Object, TimeProvider.System);

    [Fact]
    public async Task Uses_requested_day_when_capacity_available()
    {
        var r = await Sut().BookAsync("Budi", "0812", Day1, default);
        Assert.Equal(Day1, r.Date);
        Assert.Equal(1, r.TokenNumber);
        Assert.False(r.MovedFromRequestedDate);
    }

    [Fact]
    public async Task Moves_to_next_day_when_day_is_full()
    {
        _appts.Setup(a => a.CountForDateAsync(Day1, It.IsAny<CancellationToken>())).ReturnsAsync(2);
        var r = await Sut().BookAsync("Budi", "0812", Day1, default);
        Assert.Equal(Day1.AddDays(1), r.Date);
        Assert.True(r.MovedFromRequestedDate);
    }

    [Fact]
    public async Task Skips_off_day()
    {
        _config.Setup(c => c.IsOffDayAsync(Day1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var r = await Sut().BookAsync("Budi", "0812", Day1, default);
        Assert.Equal(Day1.AddDays(1), r.Date);
    }

    [Fact]
    public async Task Rejects_past_date()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            Sut().BookAsync("Budi", "0812", new DateOnly(2020, 1, 1), default));
    }

    [Fact]
    public async Task Retries_after_slot_conflict_and_succeeds()
    {
        _appts.SetupSequence(a => a.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()))
              .ThrowsAsync(new SlotConflictException("taken"))
              .Returns(Task.CompletedTask);

        var r = await Sut().BookAsync("Budi", "0812", Day1, default);

        Assert.Equal(Day1, r.Date);
        _appts.Verify(a => a.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Gives_up_with_conflict_after_max_attempts()
    {
        _appts.Setup(a => a.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()))
              .ThrowsAsync(new SlotConflictException("taken"));

        await Assert.ThrowsAsync<SlotConflictException>(() => Sut().BookAsync("Budi", "0812", Day1, default));
        _appts.Verify(a => a.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
    }
}
