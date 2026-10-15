using Moq;
using Qwiik.Appointments.Core;
using Qwiik.Appointments.Core.Entities;

public class AgencyServiceTests
{
    private readonly Mock<IAppointmentRepository> _appts = new();
    private readonly Mock<IAgencyConfigRepository> _config = new();
    private AgencyService Sut() => new(_appts.Object, _config.Object);

    [Fact]
    public async Task Queue_is_mapped_from_appointments()
    {
        var day = new DateOnly(2099, 1, 5);
        _appts.Setup(a => a.GetQueueAsync(day, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<Appointment>
              {
                  new() { TokenNumber = 1, Customer = new Customer { Name = "Budi", Phone = "0812" } }
              });

        var item = Assert.Single(await Sut().GetQueueAsync(day, default));

        Assert.Equal(1, item.Token);
        Assert.Equal("Budi", item.CustomerName);
        Assert.Equal("Booked", item.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task SetMax_rejects_non_positive_values(int max) =>
        await Assert.ThrowsAsync<BusinessRuleException>(() => Sut().SetMaxPerDayAsync(max, default));

    [Fact]
    public async Task AddOffDay_rejects_duplicate()
    {
        _config.Setup(c => c.AddOffDayAsync(It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            Sut().AddOffDayAsync(new DateOnly(2099, 1, 1), "Libur", default));
    }
}
