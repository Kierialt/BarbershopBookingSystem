using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BarbershopsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public BarbershopsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BarbershopListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<BarbershopListItemDto>>> GetByCity([FromQuery] int cityId,
        CancellationToken cancellationToken)
    {
        if (cityId <= 0)
            return BadRequest(new { error = "Parametr cityId jest wymagany i musi być dodatni." });

        var exists = await _db.Cities.AnyAsync(c => c.Id == cityId, cancellationToken);
        if (!exists)
            return BadRequest(new { error = "Nie znaleziono miasta o podanym id." });

        var raw = await _db.Barbershops.AsNoTracking()
            .Where(b => b.CityId == cityId)
            .Include(b => b.City)
            .Include(b => b.Services)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

        var items = raw.Select(b => new BarbershopListItemDto
        {
            Id = b.Id,
            Name = b.Name,
            Address = b.Address,
            CityId = b.CityId,
            CityName = b.City.Name,
            Latitude = b.Latitude,
            Longitude = b.Longitude,
            MinServicePrice = b.Services.Count > 0 ? b.Services.Min(s => s.Price) : null,
            DataSource = b.DataSource.ToString(),
        }).ToList();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BarbershopDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BarbershopDetailDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var b = await _db.Barbershops.AsNoTracking()
            .Include(x => x.City)
            .Include(x => x.Services)
            .Include(x => x.Barbers)
            .Include(x => x.Reviews.Where(r => r.ModerationStatus == ReviewModerationStatus.Approved))
            .ThenInclude(r => r.Client)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (b is null)
            return NotFound();

        var approvedReviews = b.Reviews
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new ReviewPublicDto
            {
                Id = r.Id,
                Rating = r.Rating,
                Comment = r.Comment,
                AuthorDisplayName = r.Client.FullName ?? r.Client.Email ?? "Klient",
                CreatedAtUtc = r.CreatedAtUtc,
            })
            .ToList();

        var dto = new BarbershopDetailDto
        {
            Id = b.Id,
            Name = b.Name,
            Description = b.Description,
            Address = b.Address,
            Phone = b.Phone,
            OpeningHoursNotes = b.OpeningHoursNotes,
            CityId = b.CityId,
            CityName = b.City.Name,
            Latitude = b.Latitude,
            Longitude = b.Longitude,
            DataSource = b.DataSource.ToString(),
            ExternalPlaceId = b.ExternalPlaceId,
            Services = b.Services.OrderBy(s => s.Name).Select(s => new ServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                DurationMinutes = s.DurationMinutes,
                Price = s.Price,
                Currency = s.Currency,
            }).ToList(),
            Barbers = b.Barbers.OrderBy(x => x.DisplayName).Select(x => new BarberSummaryDto
            {
                Id = x.Id,
                DisplayName = x.DisplayName,
                BarbershopId = x.BarbershopId,
            }).ToList(),
            ApprovedReviews = approvedReviews,
        };

        return Ok(dto);
    }
}
