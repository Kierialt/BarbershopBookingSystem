using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class BookingService : IBookingService
{
    private static readonly TimeSpan MinLeadTimeBeforeStart = TimeSpan.FromHours(1);

    private static readonly TimeZoneInfo PolandTz = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "Central European Standard Time" : "Europe/Warsaw");

    private readonly ApplicationDbContext _db;
    private readonly IAvailabilityService _availability;

    public BookingService(ApplicationDbContext db, IAvailabilityService availability)
    {
        _db = db;
        _availability = availability;
    }

    public async Task<(bool Ok, string? Error, Booking? Booking)> CreateAsync(string clientId, int barberId,
        int serviceId, DateTime startUtc, CancellationToken cancellationToken = default)
    {
        startUtc = NormalizeUtc(startUtc);
        var now = DateTime.UtcNow;
        if (startUtc < now + MinLeadTimeBeforeStart)
            return (false, "Rezerwacja musi być co najmniej na godzinę przed terminem.", null);

        var barber = await _db.Barbers.Include(b => b.Barbershop)
            .FirstOrDefaultAsync(b => b.Id == barberId, cancellationToken);
        if (barber is null)
            return (false, "Barber nie istnieje.", null);

        var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken);
        if (service is null || service.BarbershopId != barber.BarbershopId)
            return (false, "Usługa nie należy do tego salonu / barbera.", null);

        var endUtc = startUtc + TimeSpan.FromMinutes(service.DurationMinutes);
        if (await OverlapsAsync(barberId, startUtc, endUtc, excludeBookingId: null, cancellationToken))
            return (false, "Wybrany termin jest już zajęty.", null);

        var allowed = await _availability.GetAvailableSlotsUtcAsync(barberId, serviceId,
            GetPolandDateFromUtc(startUtc), cancellationToken);
        if (!allowed.Any(s => Math.Abs((s - startUtc).TotalMinutes) < 1))
            return (false, "Termin nie wypada w dostępnych slotach pracy salonu.", null);

        var booking = new Booking
        {
            ClientId = clientId,
            BarberId = barberId,
            ServiceId = serviceId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Status = BookingStatus.Confirmed,
            CreatedAtUtc = now,
        };
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null, booking);
    }

    public async Task<(bool Ok, string? Error)> CancelAsync(string clientId, int bookingId,
        CancellationToken cancellationToken = default)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        if (booking is null)
            return (false, "Rezerwacja nie istnieje.");
        if (booking.ClientId != clientId)
            return (false, "Brak dostępu do tej rezerwacji.");
        if (booking.Status == BookingStatus.Cancelled)
            return (false, "Rezerwacja jest już anulowana.");

        if (booking.StartUtc < DateTime.UtcNow + MinLeadTimeBeforeStart)
            return (false, "Anulowanie możliwe najpóźniej godzinę przed wizytą.");

        booking.Status = BookingStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> RescheduleAsync(string clientId, int bookingId, DateOnly newDay,
        DateTime newStartUtc, CancellationToken cancellationToken = default)
    {
        newStartUtc = NormalizeUtc(newStartUtc);
        var startPolandDate = GetPolandDateFromUtc(newStartUtc);
        if (startPolandDate != newDay)
            return (false,
                "Pole newDay musi być tym samym dniem kalendarzowym co newStartUtc w strefie Europe/Warsaw.");

        var booking = await _db.Bookings
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        if (booking is null)
            return (false, "Rezerwacja nie istnieje.");
        if (booking.ClientId != clientId)
            return (false, "Brak dostępu do tej rezerwacji.");
        if (booking.Status == BookingStatus.Cancelled)
            return (false, "Nie można zmienić anulowanej rezerwacji.");

        if (booking.StartUtc < DateTime.UtcNow + MinLeadTimeBeforeStart)
            return (false, "Zmiana terminu możliwa najpóźniej godzinę przed wizytą.");

        var now = DateTime.UtcNow;
        if (newStartUtc < now + MinLeadTimeBeforeStart)
            return (false, "Nowy termin musi być co najmniej za godzinę.");

        var newEnd = newStartUtc + TimeSpan.FromMinutes(booking.Service.DurationMinutes);
        if (await OverlapsAsync(booking.BarberId, newStartUtc, newEnd, excludeBookingId: booking.Id,
                cancellationToken))
            return (false, "Wybrany termin jest już zajęty.");

        var allowed = await _availability.GetAvailableSlotsUtcAsync(booking.BarberId, booking.ServiceId, newDay,
            cancellationToken);
        if (!allowed.Any(s => Math.Abs((s - newStartUtc).TotalMinutes) < 1))
            return (false, "Termin nie wypada w dostępnych slotach pracy salonu.");

        booking.StartUtc = newStartUtc;
        booking.EndUtc = newEnd;
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    private async Task<bool> OverlapsAsync(int barberId, DateTime startUtc, DateTime endUtc, int? excludeBookingId,
        CancellationToken cancellationToken)
    {
        var q = _db.Bookings.AsNoTracking()
            .Where(b => b.BarberId == barberId && b.Status == BookingStatus.Confirmed)
            .Where(b => b.StartUtc < endUtc && b.EndUtc > startUtc);
        if (excludeBookingId is { } id)
            q = q.Where(b => b.Id != id);
        return await q.AnyAsync(cancellationToken);
    }

    private static DateTime NormalizeUtc(DateTime dt) =>
        dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) :
        dt.Kind == DateTimeKind.Local ? dt.ToUniversalTime() : dt;

    private static DateOnly GetPolandDateFromUtc(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(NormalizeUtc(utc), PolandTz));
}
