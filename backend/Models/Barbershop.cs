namespace backend.Models;

public class Barbershop
{
    public int Id { get; set; }
    public int CityId { get; set; }
    public City City { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Phone { get; set; }

    /// <summary>Tekstowe godziny otwarcia (MVP); później można rozbić na encję harmonogramu.</summary>
    public string OpeningHoursNotes { get; set; } = string.Empty;

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>Identyfikator miejsca z Google Places — wypełniany przy imporcie z API.</summary>
    public string? ExternalPlaceId { get; set; }

    public BarbershopDataSource DataSource { get; set; }

    public ICollection<Barber> Barbers { get; set; } = new List<Barber>();
    public ICollection<Service> Services { get; set; } = new List<Service>();
}
