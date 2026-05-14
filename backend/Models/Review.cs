namespace backend.Models;

public class Review
{
    public int Id { get; set; }
    public int BarbershopId { get; set; }
    public Barbershop Barbershop { get; set; } = null!;

    public string ClientId { get; set; } = string.Empty;
    public ApplicationUser Client { get; set; } = null!;

    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public ReviewModerationStatus ModerationStatus { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
