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
[Route("api/shops/{shopId:int}/services")]
[Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Wlasciciel}")]
public class ShopServicesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IShopAuthorizationService _shopAuth;

    public ShopServicesController(ApplicationDbContext db, IShopAuthorizationService shopAuth)
    {
        _db = db;
        _shopAuth = shopAuth;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ServiceDto>>> List(int shopId, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
            return Unauthorized();
        if (!await _shopAuth.CanManageShopAsync(userId, HttpUserRoles.GetRoles(User), shopId, cancellationToken))
            return Forbid();

        var list = await _db.Services.AsNoTracking()
            .Where(s => s.BarbershopId == shopId)
            .OrderBy(s => s.Name)
            .Select(s => new ServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                DurationMinutes = s.DurationMinutes,
                Price = s.Price,
                Currency = s.Currency,
            })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ServiceDto>> Create(int shopId, [FromBody] ServiceUpsertRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        var userId = UserId();
        if (userId is null)
            return Unauthorized();
        if (!await _shopAuth.CanManageShopAsync(userId, HttpUserRoles.GetRoles(User), shopId, cancellationToken))
            return Forbid();
        if (request.Price <= 0 || request.DurationMinutes <= 0)
            return BadRequest(new { error = "Cena i czas muszą być dodatnie." });

        var entity = new Service
        {
            BarbershopId = shopId,
            Name = request.Name.Trim(),
            DurationMinutes = request.DurationMinutes,
            Price = request.Price,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "PLN" : request.Currency.Trim().ToUpperInvariant(),
        };
        _db.Services.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        var dto = new ServiceDto
        {
            Id = entity.Id,
            Name = entity.Name,
            DurationMinutes = entity.DurationMinutes,
            Price = entity.Price,
            Currency = entity.Currency,
        };
        return Created($"/api/shops/{shopId}/services/{entity.Id}", dto);
    }

    [HttpPut("{serviceId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int shopId, int serviceId, [FromBody] ServiceUpsertRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        var userId = UserId();
        if (userId is null)
            return Unauthorized();
        if (!await _shopAuth.CanManageShopAsync(userId, HttpUserRoles.GetRoles(User), shopId, cancellationToken))
            return Forbid();

        var entity = await _db.Services.FirstOrDefaultAsync(
            s => s.Id == serviceId && s.BarbershopId == shopId, cancellationToken);
        if (entity is null)
            return NotFound();
        if (request.Price <= 0 || request.DurationMinutes <= 0)
            return BadRequest(new { error = "Cena i czas muszą być dodatnie." });

        entity.Name = request.Name.Trim();
        entity.DurationMinutes = request.DurationMinutes;
        entity.Price = request.Price;
        entity.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "PLN" : request.Currency.Trim().ToUpperInvariant();
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{serviceId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int shopId, int serviceId, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
            return Unauthorized();
        if (!await _shopAuth.CanManageShopAsync(userId, HttpUserRoles.GetRoles(User), shopId, cancellationToken))
            return Forbid();

        var entity = await _db.Services.FirstOrDefaultAsync(
            s => s.Id == serviceId && s.BarbershopId == shopId, cancellationToken);
        if (entity is null)
            return NotFound();

        var hasBookings = await _db.Bookings.AnyAsync(b => b.ServiceId == serviceId, cancellationToken);
        if (hasBookings)
            return Conflict(new { error = "Nie można usunąć usługi powiązanej z rezerwacjami." });

        _db.Services.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private string? UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
}
