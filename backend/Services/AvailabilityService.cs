using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class AvailabilityService : IAvailabilityService
{
    private static readonly TimeZoneInfo PolandTz =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Central European Standard Time" : "Europe/Warsaw");

    private static readonly TimeSpan WorkdayStart = new(9, 0, 0);
    private static readonly TimeSpan WorkdayEnd = new(18, 0, 0);
    private static readonly TimeSpan SlotStep = TimeSpan.FromMinutes(30);

    private readonly ApplicationDbContext _db;

    public AvailabilityService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<DateTime>> GetAvailableSlotsUtcAsync(int barberId, int serviceId,
        DateOnly localDate, CancellationToken cancellationToken = default)
    {
        var barber = await _db.Barbers.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == barberId, cancellationToken);
        if (barber is null)
            return Array.Empty<DateTime>();

        var service = await _db.Services.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken);
        if (service is null || service.BarbershopId != barber.BarbershopId)
            return Array.Empty<DateTime>();

        var duration = TimeSpan.FromMinutes(service.DurationMinutes);
        if (duration <= TimeSpan.Zero)
            return Array.Empty<DateTime>();

        var dayStartUnspecified = localDate.ToDateTime(TimeOnly.MinValue);
        var dayStartLocal = DateTime.SpecifyKind(dayStartUnspecified, DateTimeKind.Unspecified);
        var workOpen = dayStartLocal + WorkdayStart;
        var workClose = dayStartLocal + WorkdayEnd;

        var workOpenUtc = TimeZoneInfo.ConvertTimeToUtc(workOpen, PolandTz);
        var workCloseUtc = TimeZoneInfo.ConvertTimeToUtc(workClose, PolandTz);

        var bookings = await _db.Bookings.AsNoTracking()
            .Where(b => b.BarberId == barberId && b.Status == BookingStatus.Confirmed)
            .Where(b => b.StartUtc < workCloseUtc && b.EndUtc > workOpenUtc)
            .Select(b => new { b.StartUtc, b.EndUtc })
            .ToListAsync(cancellationToken);

        var slots = new List<DateTime>();
        for (var candidateLocal = workOpen; candidateLocal + duration <= workClose; candidateLocal += SlotStep)
        {
            var candidateUnspecified = DateTime.SpecifyKind(candidateLocal, DateTimeKind.Unspecified);
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(candidateUnspecified, PolandTz);
            var endUtc = startUtc + duration;

            if (startUtc < DateTime.UtcNow)
                continue;

            var overlaps = bookings.Any(b => startUtc < b.EndUtc && endUtc > b.StartUtc);
            if (!overlaps)
                slots.Add(startUtc);
        }

        return slots;
    }
}
