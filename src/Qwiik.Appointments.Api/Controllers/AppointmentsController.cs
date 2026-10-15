using Microsoft.AspNetCore.Mvc;
using Qwiik.Appointments.Core;
namespace Qwiik.Appointments.Api.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly IBookingService _booking;
    public AppointmentsController(IBookingService booking) => _booking = booking;

    /// <summary>Books an appointment and issues a token. Moves to the next open day if needed.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BookingResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Book(BookAppointmentRequest request, CancellationToken ct)
    {
        var result = await _booking.BookAsync(request.Name, request.Phone, request.Date, ct);
        return Created($"/api/queue?date={result.Date:yyyy-MM-dd}", result);
    }
}
