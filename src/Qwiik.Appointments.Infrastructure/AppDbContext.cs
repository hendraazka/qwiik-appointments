using Microsoft.EntityFrameworkCore;
using Qwiik.Appointments.Core.Entities;
namespace Qwiik.Appointments.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<OffDay> OffDays => Set<OffDay>();
    public DbSet<AgencySetting> AgencySettings => Set<AgencySetting>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Appointment>().HasIndex(a => new { a.Date, a.TokenNumber }).IsUnique();
        b.Entity<OffDay>().HasIndex(o => o.Date).IsUnique();
        b.Entity<AgencySetting>().HasData(new AgencySetting { Id = 1, MaxAppointmentsPerDay = 20 });
    }
}
