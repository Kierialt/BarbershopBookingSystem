using backend.Models;

namespace backend.Services;

public interface IBookingService
{
    Task<(bool Ok, string? Error, Booking? Booking)> CreateAsync(string clientId, int barberId, int serviceId,
        DateTime startUtc, CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> CancelAsync(string clientId, int bookingId,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> RescheduleAsync(string clientId, int bookingId, DateTime newStartUtc,
        CancellationToken cancellationToken = default);
}
