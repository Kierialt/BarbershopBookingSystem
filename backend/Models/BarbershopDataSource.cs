namespace backend.Models;

/// <summary>
/// Źródło rekordu salonu. Zewnętrzne importy używają <see cref="Barbershop.ExternalPlaceId"/>.
/// </summary>
public enum BarbershopDataSource
{
    Seed = 0,
    GooglePlaces = 1,
    OpenStreetMap = 2,
}
