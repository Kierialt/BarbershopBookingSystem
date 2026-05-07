namespace backend.Services;

/// <summary>
/// Kontrakt pod przyszłą integrację z Google Places API (lub innym dostawcą).
/// Implementacja wstrzykiwana opcjonalnie — seed nie wymaga tego interfejsu.
/// </summary>
public interface IExternalBarbershopCatalog
{
    Task<IReadOnlyList<ExternalPlaceCandidate>> SearchPlacesInCityAsync(
        string cityDisplayName,
        CancellationToken cancellationToken = default);
}

/// <summary>Kandydat z API zewnętrznego — mapowanie do <see cref="Models.Barbershop"/> przy imporcie.</summary>
public sealed record ExternalPlaceCandidate(
    string ExternalPlaceId,
    string Name,
    string? FormattedAddress,
    double? Latitude,
    double? Longitude);
