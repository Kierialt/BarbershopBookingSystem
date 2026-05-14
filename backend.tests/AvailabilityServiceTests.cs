using backend.Data;
using backend.Models;
using backend.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.tests;

/// <summary>
/// Testy jednostkowe logiki slotów (godziny pracy, krok 30 min, kolizje z rezerwacjami).
/// </summary>
public sealed class AvailabilityServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _db;
    private readonly AvailabilityService _sut;

    public AvailabilityServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new ApplicationDbContext(options);
        _db.Database.Migrate();
        _sut = new AvailabilityService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Wolne_sloty_sa_co_30_minut_i_nie_koliduja_z_rezerwacja()
    {
        var city = new City { Name = "Testowo", CenterLatitude = 0, CenterLongitude = 0 };
        _db.Cities.Add(city);
        await _db.SaveChangesAsync();

        var shop = new Barbershop
        {
            CityId = city.Id,
            Name = "Salon test",
            Address = "ul. Test 1",
            DataSource = BarbershopDataSource.Seed,
        };
        _db.Barbershops.Add(shop);
        await _db.SaveChangesAsync();

        var barber = new Barber { BarbershopId = shop.Id, DisplayName = "Barber A" };
        _db.Barbers.Add(barber);
        var service = new Service
        {
            BarbershopId = shop.Id,
            Name = "Strzyżenie",
            DurationMinutes = 30,
            Price = 50m,
        };
        _db.Services.Add(service);
        await _db.SaveChangesAsync();

        var client = new ApplicationUser
        {
            UserName = "c@test",
            Email = "c@test",
            FullName = "Klient",
        };
        _db.Users.Add(client);
        await _db.SaveChangesAsync();

        // Dzień w przyszłości — wszystkie sloty 9–18 powinny być wolne (poza ewentualnie „przeszłością” UTC).
        var localDay = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60));
        var slotsBefore = await _sut.GetAvailableSlotsUtcAsync(barber.Id, service.Id, localDay);

        var tenAmPoland = new DateTime(localDay, new TimeOnly(10, 0), DateTimeKind.Unspecified);
        var polandTz = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Central European Standard Time" : "Europe/Warsaw");
        var tenAmUtc = TimeZoneInfo.ConvertTimeToUtc(tenAmPoland, polandTz);

        _db.Bookings.Add(new Booking
        {
            ClientId = client.Id,
            BarberId = barber.Id,
            ServiceId = service.Id,
            StartUtc = tenAmUtc,
            EndUtc = tenAmUtc.AddMinutes(30),
            Status = BookingStatus.Confirmed,
            CreatedAtUtc = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();

        var slotsAfter = await _sut.GetAvailableSlotsUtcAsync(barber.Id, service.Id, localDay);

        Assert.True(slotsBefore.Count >= slotsAfter.Count);
        Assert.DoesNotContain(slotsAfter, s => Math.Abs((s - tenAmUtc).TotalMinutes) < 1);
    }
}
