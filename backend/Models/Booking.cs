namespace backend.Models;

public class Booking
{
    public int Id { get; set; }

    public string ClientId { get; set; } = string.Empty;
    public ApplicationUser Client { get; set; } = null!;

    public int BarberId { get; set; }
    public Barber Barber { get; set; } = null!;

    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
