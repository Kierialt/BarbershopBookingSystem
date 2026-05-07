namespace backend.Models;

public class City
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Środek miasta — przydatne przy przyszłym wyszukiwaniu „Nearby” w Google Places.</summary>
    public double? CenterLatitude { get; set; }

    public double? CenterLongitude { get; set; }

    public ICollection<Barbershop> Barbershops { get; set; } = new List<Barbershop>();
}
