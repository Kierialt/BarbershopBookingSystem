namespace backend.Models;

public class Barber
{
    public int Id { get; set; }
    public int BarbershopId { get; set; }
    public Barbershop Barbershop { get; set; } = null!;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Po powiązaniu z kontem użytkownika w roli Barber — logowanie do panelu.</summary>
    public string? ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
