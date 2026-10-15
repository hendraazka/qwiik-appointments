// Abstractions.cs
using Qwiik.Appointments.Core.Entities;
namespace Qwiik.Appointments.Core;

public interface IAppointmentRepository
{
    Task<int> CountForDateAsync(DateOnly date, CancellationToken ct);
    Task<int> GetLastTokenAsync(DateOnly date, CancellationToken ct);
    Task AddAsync(Appointment appointment, CancellationToken ct);
    Task<List<Appointment>> GetQueueAsync(DateOnly date, CancellationToken ct);
}

public interface IAgencyConfigRepository
{
    Task<int> GetMaxPerDayAsync(CancellationToken ct);
    Task<bool> IsOffDayAsync(DateOnly date, CancellationToken ct);
    Task SetMaxPerDayAsync(int max, CancellationToken ct);
    Task<bool> AddOffDayAsync(DateOnly date, string description, CancellationToken ct);
}

public interface IBookingService
{
    Task<BookingResult> BookAsync(string name, string phone, DateOnly requestedDate, CancellationToken ct);
}

public record BookingResult(int AppointmentId, DateOnly Date, int TokenNumber, bool MovedFromRequestedDate);

public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}
