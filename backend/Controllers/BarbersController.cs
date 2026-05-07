using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BarbersController : ControllerBase
{
    private readonly IAvailabilityService _availability;

    public BarbersController(IAvailabilityService availability)
    {
        _availability = availability;
    }

    /// <summary>Wolne terminy (UTC) dla barbera i usługi w wybranym dniu kalendarzowym (Polska).</summary>
    [HttpGet("{id:int}/availability")]
    [ProducesResponseType(typeof(IReadOnlyList<DateTime>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<DateTime>>> GetAvailability(int id,
        [FromQuery] int serviceId,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        if (serviceId <= 0)
            return BadRequest(new { error = "Parametr serviceId jest wymagany." });

        var slots = await _availability.GetAvailableSlotsUtcAsync(id, serviceId, date, cancellationToken);
        return Ok(slots);
    }
}
