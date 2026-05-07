namespace backend.Dtos;

public class BarbershopListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public decimal? MinServicePrice { get; set; }
    public string DataSource { get; set; } = string.Empty;
}

public class BarbershopDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string OpeningHoursNotes { get; set; } = string.Empty;
    public int CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string DataSource { get; set; } = string.Empty;
    public string? ExternalPlaceId { get; set; }
    public IReadOnlyList<ServiceDto> Services { get; set; } = Array.Empty<ServiceDto>();
    public IReadOnlyList<BarberSummaryDto> Barbers { get; set; } = Array.Empty<BarberSummaryDto>();
}

public class ServiceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "PLN";
}

public class BarberSummaryDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int BarbershopId { get; set; }
}
