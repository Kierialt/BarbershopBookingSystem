using System.Security.Claims;
using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IBookingService _bookings;

    public BookingsController(ApplicationDbContext db, IBookingService bookings)
    {
        _db = db;
        _bookings = bookings;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(IReadOnlyList<BookingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookingDto>>> GetMine(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var list = await _db.Bookings.AsNoTracking()
            .Where(b => b.ClientId == userId)
            .Include(b => b.Barber)
            .Include(b => b.Service)
            .ThenInclude(s => s.Barbershop)
            .OrderByDescending(b => b.StartUtc)
            .ToListAsync(cancellationToken);

        return Ok(list.Select(Map).ToList());
    }

    [HttpPost]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookingDto>> Create([FromBody] CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var (ok, error, booking) =
            await _bookings.CreateAsync(userId, request.BarberId, request.ServiceId, request.StartUtc,
                cancellationToken);
        if (!ok || booking is null)
            return BadRequest(new { error });

        await _db.Entry(booking).Reference(x => x.Barber).LoadAsync(cancellationToken);
        await _db.Entry(booking).Reference(x => x.Service).LoadAsync(cancellationToken);
        await _db.Entry(booking.Service).Reference(x => x.Barbershop).LoadAsync(cancellationToken);

        var dto = Map(booking);
        return Created("/api/Bookings/me", dto);
    }

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var (ok, error) = await _bookings.CancelAsync(userId, id, cancellationToken);
        if (!ok)
            return BadRequest(new { error });

        return NoContent();
    }

    [HttpPatch("{id:int}/reschedule")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reschedule(int id, [FromBody] RescheduleBookingRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var (ok, error) = await _bookings.RescheduleAsync(userId, id, request.NewStartUtc, cancellationToken);
        if (!ok)
            return BadRequest(new { error });

        return NoContent();
    }

    private static BookingDto Map(Booking b) => new()
    {
        Id = b.Id,
        BarberId = b.BarberId,
        BarberName = b.Barber.DisplayName,
        ServiceId = b.ServiceId,
        ServiceName = b.Service.Name,
        BarbershopId = b.Service.BarbershopId,
        BarbershopName = b.Service.Barbershop.Name,
        StartUtc = b.StartUtc,
        EndUtc = b.EndUtc,
        Status = b.Status.ToString(),
    };
}
