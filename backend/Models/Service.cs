namespace backend.Models;

public class Service
{
    public int Id { get; set; }
    public int BarbershopId { get; set; }
    public Barbershop Barbershop { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "PLN";

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
