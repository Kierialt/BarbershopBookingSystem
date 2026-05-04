using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

public class CreateBookingRequest
{
    [Required]
    public int BarberId { get; set; }

    [Required]
    public int ServiceId { get; set; }

    /// <summary>Początek wizyty w UTC (ISO 8601), np. 2026-05-10T12:00:00Z</summary>
    [Required]
    public DateTime StartUtc { get; set; }
}

public class RescheduleBookingRequest
{
    [Required]
    public DateTime NewStartUtc { get; set; }
}

public class BookingDto
{
    public int Id { get; set; }
    public int BarberId { get; set; }
    public string BarberName { get; set; } = string.Empty;
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int BarbershopId { get; set; }
    public string BarbershopName { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string Status { get; set; } = string.Empty;
}
