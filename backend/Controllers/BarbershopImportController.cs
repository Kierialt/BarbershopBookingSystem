using backend.Data;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/barbershop-import")]
public class BarbershopImportController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IExternalBarbershopCatalog _externalCatalog;

    public BarbershopImportController(ApplicationDbContext db, IExternalBarbershopCatalog externalCatalog)
    {
        _db = db;
        _externalCatalog = externalCatalog;
    }

    [HttpPost("osm")]
    [ProducesResponseType(typeof(ImportSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportSummaryDto>> ImportOsm([FromQuery] int cityId,
        CancellationToken cancellationToken)
    {
        if (cityId <= 0)
            return BadRequest(new { error = "Parametr cityId jest wymagany i musi być dodatni." });

        var city = await _db.Cities.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == cityId, cancellationToken);
        if (city is null)
            return BadRequest(new { error = "Nie znaleziono miasta o podanym id." });

        var candidates = await _externalCatalog.SearchPlacesInCityAsync(city.Name, cancellationToken);
        if (candidates.Count == 0)
        {
            return Ok(new ImportSummaryDto(city.Id, city.Name, 0, 0, 0));
        }

        var candidateIds = candidates
            .Select(x => x.ExternalPlaceId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingByExternalId = await _db.Barbershops
            .Where(x => x.ExternalPlaceId != null && candidateIds.Contains(x.ExternalPlaceId))
            .ToDictionaryAsync(x => x.ExternalPlaceId!, x => x, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var inserted = 0;
        var updated = 0;

        foreach (var candidate in candidates)
        {
            if (existingByExternalId.TryGetValue(candidate.ExternalPlaceId, out var existing))
            {
                existing.Name = candidate.Name;
                existing.Address = candidate.FormattedAddress ?? existing.Address;
                existing.Latitude = candidate.Latitude;
                existing.Longitude = candidate.Longitude;
                existing.DataSource = BarbershopDataSource.OpenStreetMap;
                updated++;
                continue;
            }

            _db.Barbershops.Add(new Barbershop
            {
                CityId = city.Id,
                Name = candidate.Name,
                Description = "Zaimportowano automatycznie z OpenStreetMap (Overpass API).",
                Address = candidate.FormattedAddress ?? city.Name,
                OpeningHoursNotes = "Brak danych",
                Latitude = candidate.Latitude,
                Longitude = candidate.Longitude,
                ExternalPlaceId = candidate.ExternalPlaceId,
                DataSource = BarbershopDataSource.OpenStreetMap,
            });
            inserted++;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new ImportSummaryDto(
            city.Id,
            city.Name,
            candidates.Count,
            inserted,
            updated));
    }

    [HttpGet("osm-preview")]
    [ProducesResponseType(typeof(IReadOnlyList<ExternalPlaceCandidate>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ExternalPlaceCandidate>>> PreviewOsm([FromQuery] int cityId,
        CancellationToken cancellationToken)
    {
        if (cityId <= 0)
            return BadRequest(new { error = "Parametr cityId jest wymagany i musi być dodatni." });

        var city = await _db.Cities.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == cityId, cancellationToken);
        if (city is null)
            return BadRequest(new { error = "Nie znaleziono miasta o podanym id." });

        var candidates = await _externalCatalog.SearchPlacesInCityAsync(city.Name, cancellationToken);
        return Ok(candidates);
    }
}

public sealed record ImportSummaryDto(
    int CityId,
    string CityName,
    int Fetched,
    int Inserted,
    int Updated);
