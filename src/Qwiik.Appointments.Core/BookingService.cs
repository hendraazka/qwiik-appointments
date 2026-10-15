// BookingService.cs
using Qwiik.Appointments.Core.Entities;
namespace Qwiik.Appointments.Core;

public class BookingService : IBookingService
{
    private const int MaxLookAheadDays = 365;
    private readonly IAppointmentRepository _appointments;
    private readonly IAgencyConfigRepository _config;
    private readonly TimeProvider _time;

    public BookingService(IAppointmentRepository appointments, IAgencyConfigRepository config, TimeProvider time)
        => (_appointments, _config, _time) = (appointments, config, time);

    public async Task<BookingResult> BookAsync(string name, string phone, DateOnly requestedDate, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessRuleException("Customer name is required.");

        var today = DateOnly.FromDateTime(_time.GetLocalNow().DateTime);
        if (requestedDate < today) throw new BusinessRuleException("Cannot book an appointment in the past.");

        var max = await _config.GetMaxPerDayAsync(ct);
        var date = requestedDate;

        for (var i = 0; i < MaxLookAheadDays; i++, date = date.AddDays(1))
        {
            if (await _config.IsOffDayAsync(date, ct)) continue;
            if (await _appointments.CountForDateAsync(date, ct) >= max) continue;

            var token = await _appointments.GetLastTokenAsync(date, ct) + 1;
            var appointment = new Appointment
            {
                Customer = new Customer { Name = name.Trim(), Phone = phone.Trim() },
                Date = date,
                TokenNumber = token,
                CreatedAtUtc = _time.GetUtcNow().UtcDateTime
            };
            await _appointments.AddAsync(appointment, ct);
            return new BookingResult(appointment.Id, date, token, date != requestedDate);
        }

        throw new BusinessRuleException("No available slot within the next 365 days.");
    }
}
