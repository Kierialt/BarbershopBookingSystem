using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

public class ServiceUpsertRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Range(5, 480)]
    public int DurationMinutes { get; set; }

    public decimal Price { get; set; }

    [StringLength(8)]
    public string Currency { get; set; } = "PLN";
}

public class BarberCreateRequest
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string DisplayName { get; set; } = string.Empty;
}

public class BookingStaffRowDto
{
    public int Id { get; set; }
    public string ClientEmail { get; set; } = string.Empty;
    public string? ClientFullName { get; set; }
    public int BarberId { get; set; }
    public string BarberName { get; set; } = string.Empty;
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateReviewRequest
{
    [Range(1, 5)]
    public int Rating { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 3)]
    public string Comment { get; set; } = string.Empty;
}

public class ReviewModerateRequest
{
    /// <summary><c>true</c> — zatwierdź, <c>false</c> — odrzuć (ukryj).</summary>
    public bool Approve { get; set; }
}

public class ReviewPendingDto
{
    public int Id { get; set; }
    public int BarbershopId { get; set; }
    public string BarbershopName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public string? ClientFullName { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string ModerationStatus { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public class ReviewPublicDto
{
    public int Id { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string AuthorDisplayName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
