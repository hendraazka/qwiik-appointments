using Microsoft.AspNetCore.Mvc;
using Qwiik.Appointments.Core;
namespace Qwiik.Appointments.Api.Controllers;

[ApiController]
[Route("api")]
public class AgencyController : ControllerBase
{
    private readonly IAgencyService _agency;
    private readonly TimeProvider _time;
    public AgencyController(IAgencyService agency, TimeProvider time) => (_agency, _time) = (agency, time);

    /// <summary>Queue grid for a day, ordered by token. Defaults to today.</summary>
    [HttpGet("queue")]
    public async Task<ActionResult<List<QueueItem>>> GetQueue([FromQuery] DateOnly? date, CancellationToken ct)
        => await _agency.GetQueueAsync(date ?? DateOnly.FromDateTime(_time.GetLocalNow().DateTime), ct);

    /// <summary>Sets the maximum appointments allowed per day.</summary>
    [HttpPut("settings/max-per-day")]
    public async Task<IActionResult> SetMax(SetMaxRequest request, CancellationToken ct)
    {
        await _agency.SetMaxPerDayAsync(request.MaxAppointmentsPerDay, ct);
        return NoContent();
    }

    /// <summary>Registers an off day (e.g. public holiday).</summary>
    [HttpPost("off-days")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddOffDay(AddOffDayRequest request, CancellationToken ct)
    {
        await _agency.AddOffDayAsync(request.Date, request.Description, ct);
        return NoContent();
    }
}
