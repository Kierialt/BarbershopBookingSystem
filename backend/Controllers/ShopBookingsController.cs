using System.Security.Claims;
using backend.Data;
using backend.Dtos;
using backend.Infrastructure;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/shops/{shopId:int}/bookings")]
[Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Wlasciciel},{RoleNames.Barber}")]
public class ShopBookingsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IShopAuthorizationService _shopAuth;
    private readonly IBookingService _bookings;

    public ShopBookingsController(
        ApplicationDbContext db,
        IShopAuthorizationService shopAuth,
        IBookingService bookings)
    {
        _db = db;
        _shopAuth = shopAuth;
        _bookings = bookings;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BookingStaffRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<BookingStaffRowDto>>> List(int shopId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var roles = HttpUserRoles.GetRoles(User);
        var canManage = await _shopAuth.CanManageShopAsync(userId, roles, shopId, cancellationToken);
        int? barberId = null;
        if (!canManage)
        {
            barberId = await _shopAuth.GetBarberIdForUserAsync(userId, cancellationToken);
            if (barberId is null)
                return Forbid();
            var atShop = await _db.Barbers.AnyAsync(
                b => b.Id == barberId && b.BarbershopId == shopId, cancellationToken);
            if (!atShop)
                return Forbid();
        }

        var q = _db.Bookings.AsNoTracking()
            .Where(b => b.Service.BarbershopId == shopId);
        if (!canManage)
            q = q.Where(b => b.BarberId == barberId!.Value);

        var list = await q
            .Include(b => b.Client)
            .Include(b => b.Barber)
            .Include(b => b.Service)
            .OrderByDescending(b => b.StartUtc)
            .ToListAsync(cancellationToken);

        return Ok(list.Select(b => new BookingStaffRowDto
        {
            Id = b.Id,
            ClientEmail = b.Client.Email ?? string.Empty,
            ClientFullName = b.Client.FullName,
            BarberId = b.BarberId,
            BarberName = b.Barber.DisplayName,
            ServiceId = b.ServiceId,
            ServiceName = b.Service.Name,
            StartUtc = b.StartUtc,
            EndUtc = b.EndUtc,
            Status = b.Status.ToString(),
        }).ToList());
    }

    [HttpPost("{bookingId:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StaffCancel(int shopId, int bookingId, CancellationToken cancellationToken)
    {
        if (!await BookingBelongsToShopAsync(shopId, bookingId, cancellationToken))
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var (ok, error) = await _bookings.StaffCancelAsync(userId, HttpUserRoles.GetRoles(User), bookingId,
            cancellationToken);
        if (!ok)
            return BadRequest(new { error });

        return NoContent();
    }

    [HttpPatch("{bookingId:int}/reschedule")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StaffReschedule(int shopId, int bookingId,
        [FromBody] RescheduleBookingRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        if (!await BookingBelongsToShopAsync(shopId, bookingId, cancellationToken))
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var (ok, error) = await _bookings.StaffRescheduleAsync(userId, HttpUserRoles.GetRoles(User), bookingId,
            request.NewDay, request.NewStartUtc, cancellationToken);
        if (!ok)
            return BadRequest(new { error });

        return NoContent();
    }

    private Task<bool> BookingBelongsToShopAsync(int shopId, int bookingId, CancellationToken cancellationToken) =>
        _db.Bookings.AsNoTracking()
            .AnyAsync(b => b.Id == bookingId && b.Service.BarbershopId == shopId, cancellationToken);
}
