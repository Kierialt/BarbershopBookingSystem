using Microsoft.AspNetCore.Identity;

namespace backend.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }

    public ICollection<Booking> ClientBookings { get; set; } = new List<Booking>();
    public ICollection<Barber> BarberProfiles { get; set; } = new List<Barber>();
}
