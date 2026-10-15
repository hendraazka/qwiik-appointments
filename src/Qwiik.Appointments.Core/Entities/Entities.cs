namespace Qwiik.Appointments.Core.Entities;

public enum AppointmentStatus { Booked = 0, Cancelled = 1 }

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
}

public class Appointment
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public DateOnly Date { get; set; }
    public int TokenNumber { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Booked;
    public DateTime CreatedAtUtc { get; set; }
}

public class OffDay
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Description { get; set; } = "";
}

public class AgencySetting
{
    public int Id { get; set; }
    public int MaxAppointmentsPerDay { get; set; }
}
