using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace backend.tests;

/// <summary>
/// Testy integracyjne API odpowiadające scenariuszom funkcjonalnym: rejestracja, logowanie,
/// wyszukiwanie salonów, usługi, dostępność, rezerwacja i anulowanie.
/// </summary>
public sealed class ApiIntegrationTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonWeb = new(JsonSerializerDefaults.Web);

    private readonly BarbershopApiFactory _factory = new();
    private readonly HttpClient _client;

    public ApiIntegrationTests()
    {
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Lista_miast_zwraca_Warszawe_po_seedzie()
    {
        var cities = await _client.GetFromJsonAsync<List<CityListItem>>("/api/Cities", JsonWeb);
        Assert.NotNull(cities);
        Assert.Contains(cities, c => c.Name == "Warszawa");
    }

    [Fact]
    public async Task Rejestracja_zwraca_JWT_i_logowanie_tymi_samymi_danymi_dziala()
    {
        var email = $"user_{Guid.NewGuid():N}@test.local";
        const string password = "HasloTestowe1";

        var registerBody = new RegisterRequest
        {
            FullName = "Jan Testowy",
            Email = email,
            Password = password,
            ConfirmPassword = password,
        };

        var reg = await _client.PostAsJsonAsync("/api/Auth/register", registerBody, JsonWeb);
        Assert.Equal(HttpStatusCode.OK, reg.StatusCode);
        var auth = await reg.Content.ReadFromJsonAsync<AuthResponse>(JsonWeb);
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
        Assert.Equal(email, auth.Email);

        var loginBody = new LoginRequest { Email = email, Password = password };
        var log = await _client.PostAsJsonAsync("/api/Auth/login", loginBody, JsonWeb);
        Assert.Equal(HttpStatusCode.OK, log.StatusCode);
        var auth2 = await log.Content.ReadFromJsonAsync<AuthResponse>(JsonWeb);
        Assert.NotNull(auth2);
        Assert.False(string.IsNullOrWhiteSpace(auth2.Token));
    }

    [Fact]
    public async Task Rejestracja_powtorzony_email_zwraca_400()
    {
        var email = $"dup_{Guid.NewGuid():N}@test.local";
        const string password = "HasloTestowe1";
        var body = new RegisterRequest
        {
            FullName = "Klient Duplikat",
            Email = email,
            Password = password,
            ConfirmPassword = password,
        };

        var first = await _client.PostAsJsonAsync("/api/Auth/register", body, JsonWeb);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/Auth/register", body, JsonWeb);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Logowanie_zle_haslo_zwraca_401()
    {
        var email = $"badlogin_{Guid.NewGuid():N}@test.local";
        const string password = "HasloTestowe1";
        await _client.PostAsJsonAsync("/api/Auth/register",
            new RegisterRequest
            {
                FullName = "Test",
                Email = email,
                Password = password,
                ConfirmPassword = password,
            }, JsonWeb);

        var log = await _client.PostAsJsonAsync("/api/Auth/login",
            new LoginRequest { Email = email, Password = "ZleHaslo1" }, JsonWeb);
        Assert.Equal(HttpStatusCode.Unauthorized, log.StatusCode);
    }

    [Fact]
    public async Task Wyszukiwanie_salonow_po_miescie_zwraca_liste()
    {
        var cities = await _client.GetFromJsonAsync<List<CityListItem>>("/api/Cities", JsonWeb);
        var warsawId = cities!.First(c => c.Name == "Warszawa").Id;

        var list = await _client.GetFromJsonAsync<List<BarbershopListItem>>(
            $"/api/Barbershops?cityId={warsawId}", JsonWeb);

        Assert.NotNull(list);
        Assert.NotEmpty(list);
        Assert.All(list, x => Assert.Equal(warsawId, x.CityId));
    }

    [Fact]
    public async Task Szczegoly_salonu_zawieraja_uslugi_i_barberow()
    {
        var cities = await _client.GetFromJsonAsync<List<CityListItem>>("/api/Cities", JsonWeb);
        var warsawId = cities!.First(c => c.Name == "Warszawa").Id;
        var list = await _client.GetFromJsonAsync<List<BarbershopListItem>>(
            $"/api/Barbershops?cityId={warsawId}", JsonWeb);
        var shopId = list!.First().Id;

        var detail = await _client.GetFromJsonAsync<BarbershopDetailTestDto>(
            $"/api/Barbershops/{shopId}", JsonWeb);

        Assert.NotNull(detail);
        Assert.NotEmpty(detail.Services);
        Assert.NotEmpty(detail.Barbers);
    }

    [Fact]
    public async Task Dostepnosc_zwraca_sloty_w_przyszlosci_dla_poprawnej_pary_barber_usluga()
    {
        var (barberId, serviceId) = await GetFirstBarberAndServiceIdsAsync();
        var (day, slot) = await FindBookableSlotAsync(barberId, serviceId);
        Assert.True(slot >= DateTime.UtcNow.AddHours(1));
        Assert.NotEqual(default, day);
    }

    [Fact]
    public async Task Pelny_przeplyw_rezerwacja_i_anulowanie()
    {
        var email = $"book_{Guid.NewGuid():N}@test.local";
        const string password = "HasloTestowe1";
        var reg = await _client.PostAsJsonAsync("/api/Auth/register",
            new RegisterRequest
            {
                FullName = "Klient Rezerwacja",
                Email = email,
                Password = password,
                ConfirmPassword = password,
            }, JsonWeb);
        var auth = await reg.Content.ReadFromJsonAsync<AuthResponse>(JsonWeb);
        Assert.NotNull(auth);

        var (barberId, serviceId) = await GetFirstBarberAndServiceIdsAsync();
        var (_, slot) = await FindBookableSlotAsync(barberId, serviceId);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.Token);

        var create = await _client.PostAsJsonAsync("/api/Bookings",
            new CreateBookingRequest { BarberId = barberId, ServiceId = serviceId, StartUtc = slot },
            JsonWeb);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var booking = await create.Content.ReadFromJsonAsync<BookingDto>(JsonWeb);
        Assert.NotNull(booking);
        Assert.Equal("Confirmed", booking.Status);

        var cancel = await _client.PostAsync($"/api/Bookings/{booking.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

        var cancelAgain = await _client.PostAsync($"/api/Bookings/{booking.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.BadRequest, cancelAgain.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persisted = await db.Bookings.AsNoTracking().FirstAsync(b => b.Id == booking.Id);
        Assert.Equal(BookingStatus.Cancelled, persisted.Status);
    }

    private async Task<(int barberId, int serviceId)> GetFirstBarberAndServiceIdsAsync()
    {
        var cities = await _client.GetFromJsonAsync<List<CityListItem>>("/api/Cities", JsonWeb);
        var warsawId = cities!.First(c => c.Name == "Warszawa").Id;
        var list = await _client.GetFromJsonAsync<List<BarbershopListItem>>(
            $"/api/Barbershops?cityId={warsawId}", JsonWeb);
        var shopId = list!.First().Id;
        var detail = await _client.GetFromJsonAsync<BarbershopDetailTestDto>(
            $"/api/Barbershops/{shopId}", JsonWeb);
        return (detail!.Barbers.First().Id, detail.Services.First().Id);
    }

    /// <summary>Znajduje dzień i slot UTC spełniający wymóg co najmniej 1 h wyprzedzenia rezerwacji.</summary>
    private async Task<(DateOnly day, DateTime slotUtc)> FindBookableSlotAsync(int barberId, int serviceId)
    {
        var minStart = DateTime.UtcNow.AddHours(1.05);
        for (var i = 1; i <= 30; i++)
        {
            var day = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(i));
            var resp = await _client.GetAsync(
                $"/api/Barbers/{barberId}/availability?serviceId={serviceId}&date={day:yyyy-MM-dd}");
            resp.EnsureSuccessStatusCode();
            var slots = await resp.Content.ReadFromJsonAsync<List<DateTime>>(JsonWeb);
            if (slots is null || slots.Count == 0)
                continue;
            var pick = slots.FirstOrDefault(s => s >= minStart);
            if (pick != default)
                return (day, DateTime.SpecifyKind(pick, DateTimeKind.Utc));
        }

        throw new InvalidOperationException("Brak wolnego slotu w testowanym horyzoncie 30 dni.");
    }

    private sealed class CityListItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private sealed class BarbershopListItem
    {
        public int Id { get; set; }
        public int CityId { get; set; }
        public string Name { get; set; } = "";
    }

    private sealed class BarbershopDetailTestDto
    {
        public List<ServiceItem> Services { get; set; } = [];
        public List<BarberItem> Barbers { get; set; } = [];
    }

    private sealed class ServiceItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private sealed class BarberItem
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = "";
    }
}
