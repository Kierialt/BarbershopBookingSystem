using System.Security.Claims;
using backend.Data;
using backend.Dtos;
using backend.Infrastructure;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/shops/{shopId:int}/barbers")]
[Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Wlasciciel}")]
public class ShopBarbersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IShopAuthorizationService _shopAuth;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _configuration;

    public ShopBarbersController(
        ApplicationDbContext db,
        IShopAuthorizationService shopAuth,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment env,
        IConfiguration configuration)
    {
        _db = db;
        _shopAuth = shopAuth;
        _userManager = userManager;
        _env = env;
        _configuration = configuration;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BarberSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<BarberSummaryDto>>> List(int shopId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        if (!await _shopAuth.CanManageShopAsync(userId, HttpUserRoles.GetRoles(User), shopId, cancellationToken))
            return Forbid();

        var list = await _db.Barbers.AsNoTracking()
            .Where(b => b.BarbershopId == shopId)
            .OrderBy(b => b.DisplayName)
            .Select(b => new BarberSummaryDto
            {
                Id = b.Id,
                DisplayName = b.DisplayName,
                BarbershopId = b.BarbershopId,
            })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BarberSummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BarberSummaryDto>> Create(int shopId, [FromBody] BarberCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        if (!await _shopAuth.CanManageShopAsync(userId, HttpUserRoles.GetRoles(User), shopId, cancellationToken))
            return Forbid();

        var barber = new Barber
        {
            BarbershopId = shopId,
            DisplayName = request.DisplayName.Trim(),
        };
        _db.Barbers.Add(barber);
        await _db.SaveChangesAsync(cancellationToken);

        if (_env.IsDevelopment() && _configuration.GetValue("Demo:SeedStaffAccounts", true))
            await DevStaffAccountsSeeder.EnsureBarberLoginForSingleAsync(
                _db, _userManager, barber.Id, _configuration, null, cancellationToken);

        return Created($"/api/shops/{shopId}/barbers/{barber.Id}", new BarberSummaryDto
        {
            Id = barber.Id,
            DisplayName = barber.DisplayName,
            BarbershopId = barber.BarbershopId,
        });
    }

    [HttpDelete("{barberId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int shopId, int barberId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        if (!await _shopAuth.CanManageShopAsync(userId, HttpUserRoles.GetRoles(User), shopId, cancellationToken))
            return Forbid();

        var barber = await _db.Barbers.FirstOrDefaultAsync(
            b => b.Id == barberId && b.BarbershopId == shopId, cancellationToken);
        if (barber is null)
            return NotFound();

        var hasBookings = await _db.Bookings.AnyAsync(
            b => b.BarberId == barberId && b.Status == BookingStatus.Confirmed, cancellationToken);
        if (hasBookings)
            return Conflict(new { error = "Nie można usunąć barbera z aktywnymi rezerwacjami." });

        _db.Barbers.Remove(barber);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
