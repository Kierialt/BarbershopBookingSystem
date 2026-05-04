namespace backend.Models;

/// <summary>
/// Źródło rekordu salonu. <see cref="GooglePlaces"/> + <see cref="Barbershop.ExternalPlaceId"/>
/// przygotowane pod późniejszy import z Google Places API bez zmiany schematu relacji.
/// </summary>
public enum BarbershopDataSource
{
    Seed = 0,
    GooglePlaces = 1,
}
