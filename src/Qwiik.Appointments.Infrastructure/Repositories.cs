using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Qwiik.Appointments.Core;
using Qwiik.Appointments.Core.Entities;
namespace Qwiik.Appointments.Infrastructure;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly AppDbContext _db;
    public AppointmentRepository(AppDbContext db) => _db = db;

    public Task<int> CountForDateAsync(DateOnly date, CancellationToken ct) =>
        _db.Appointments.CountAsync(a => a.Date == date && a.Status != AppointmentStatus.Cancelled, ct);

    public async Task<int> GetLastTokenAsync(DateOnly date, CancellationToken ct) =>
        await _db.Appointments.Where(a => a.Date == date).MaxAsync(a => (int?)a.TokenNumber, ct) ?? 0;

    public async Task AddAsync(Appointment appointment, CancellationToken ct)
    {
        _db.Appointments.Add(appointment);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: 19 } s
                                           && s.Message.Contains("UNIQUE"))
        {
            _db.ChangeTracker.Clear();
            throw new SlotConflictException("Token already taken for this date.", ex);
        }
    }

    public Task<List<Appointment>> GetQueueAsync(DateOnly date, CancellationToken ct) =>
        _db.Appointments.AsNoTracking().Include(a => a.Customer)
           .Where(a => a.Date == date && a.Status != AppointmentStatus.Cancelled)
           .OrderBy(a => a.TokenNumber).ToListAsync(ct);
}

public class AgencyConfigRepository : IAgencyConfigRepository
{
    private readonly AppDbContext _db;
    public AgencyConfigRepository(AppDbContext db) => _db = db;

    public Task<int> GetMaxPerDayAsync(CancellationToken ct) =>
        _db.AgencySettings.Select(s => s.MaxAppointmentsPerDay).FirstAsync(ct);

    public Task<bool> IsOffDayAsync(DateOnly date, CancellationToken ct) =>
        _db.OffDays.AnyAsync(o => o.Date == date, ct);

    public async Task SetMaxPerDayAsync(int max, CancellationToken ct)
    {
        var setting = await _db.AgencySettings.FirstAsync(ct);
        setting.MaxAppointmentsPerDay = max;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> AddOffDayAsync(DateOnly date, string description, CancellationToken ct)
    {
        if (await _db.OffDays.AnyAsync(o => o.Date == date, ct)) return false;
        _db.OffDays.Add(new OffDay { Date = date, Description = description });
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
