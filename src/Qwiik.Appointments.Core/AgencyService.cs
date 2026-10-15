namespace Qwiik.Appointments.Core;

public record QueueItem(int Token, string CustomerName, string Phone, string Status);

public interface IAgencyService
{
    Task<List<QueueItem>> GetQueueAsync(DateOnly date, CancellationToken ct);
    Task SetMaxPerDayAsync(int max, CancellationToken ct);
    Task AddOffDayAsync(DateOnly date, string description, CancellationToken ct);
}

public class AgencyService : IAgencyService
{
    private readonly IAppointmentRepository _appointments;
    private readonly IAgencyConfigRepository _config;

    public AgencyService(IAppointmentRepository appointments, IAgencyConfigRepository config)
        => (_appointments, _config) = (appointments, config);

    public async Task<List<QueueItem>> GetQueueAsync(DateOnly date, CancellationToken ct)
    {
        var items = await _appointments.GetQueueAsync(date, ct);
        return items
            .Select(a => new QueueItem(a.TokenNumber, a.Customer.Name, a.Customer.Phone, a.Status.ToString()))
            .ToList();
    }

    public Task SetMaxPerDayAsync(int max, CancellationToken ct)
    {
        if (max < 1) throw new BusinessRuleException("Max appointments per day must be at least 1.");
        return _config.SetMaxPerDayAsync(max, ct);
    }

    public async Task AddOffDayAsync(DateOnly date, string description, CancellationToken ct)
    {
        var added = await _config.AddOffDayAsync(date, description?.Trim() ?? "", ct);
        if (!added) throw new BusinessRuleException($"{date:yyyy-MM-dd} is already registered as an off day.");
    }
}
