using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public interface IShopAuthorizationService
{
    Task<bool> CanManageShopAsync(string userId, IReadOnlyList<string> roles, int shopId,
        CancellationToken cancellationToken = default);

    Task<bool> CanStaffAccessBookingAsync(string userId, IReadOnlyList<string> roles, int bookingId,
        CancellationToken cancellationToken = default);

    Task<int?> GetBarberIdForUserAsync(string userId, CancellationToken cancellationToken = default);
}

public class ShopAuthorizationService : IShopAuthorizationService
{
    private readonly ApplicationDbContext _db;

    public ShopAuthorizationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> CanManageShopAsync(string userId, IReadOnlyList<string> roles, int shopId,
        CancellationToken cancellationToken = default)
    {
        if (roles.Contains(RoleNames.Administrator))
            return true;
        if (!roles.Contains(RoleNames.Wlasciciel))
            return false;
        return await _db.Barbershops.AnyAsync(
            s => s.Id == shopId && s.OwnerUserId == userId, cancellationToken);
    }

    public async Task<bool> CanStaffAccessBookingAsync(string userId, IReadOnlyList<string> roles,
        int bookingId, CancellationToken cancellationToken = default)
    {
        if (roles.Contains(RoleNames.Administrator))
            return true;

        var booking = await _db.Bookings.AsNoTracking()
            .Include(b => b.Barber)
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        if (booking is null)
            return false;

        var shopId = booking.Service.BarbershopId;

        if (roles.Contains(RoleNames.Wlasciciel) &&
            await _db.Barbershops.AnyAsync(s => s.Id == shopId && s.OwnerUserId == userId, cancellationToken))
            return true;

        if (roles.Contains(RoleNames.Barber) && booking.Barber.ApplicationUserId == userId)
            return true;

        return false;
    }

    public async Task<int?> GetBarberIdForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var id = await _db.Barbers.AsNoTracking()
            .Where(b => b.ApplicationUserId == userId)
            .Select(b => b.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return id == 0 ? null : id;
    }
}
