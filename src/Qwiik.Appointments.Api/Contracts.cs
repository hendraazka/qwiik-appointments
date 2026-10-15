using System.ComponentModel.DataAnnotations;
namespace Qwiik.Appointments.Api;

public class BookAppointmentRequest
{
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, StringLength(30)] public string Phone { get; set; } = "";
    public DateOnly Date { get; set; }
}

public class SetMaxRequest
{
    [Range(1, 10000)] public int MaxAppointmentsPerDay { get; set; }
}

public class AddOffDayRequest
{
    public DateOnly Date { get; set; }
    [StringLength(200)] public string Description { get; set; } = "";
}
