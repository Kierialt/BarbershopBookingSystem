using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

/// <summary>
/// Dane startowe: nazwy i adresy pochodzą z publicznych wizytówek firm (strony WWW, katalogi).
/// Służą wyłącznie do demonstracji aplikacji — przed produkcją wymagają weryfikacji i zgód.
/// </summary>
public static class BarbershopDemoSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Cities.AnyAsync(cancellationToken))
            return;

        await InsertDemoDataAsync(db, cancellationToken);
    }

    /// <summary>
    /// Usuwa domenę barbershopów (rezerwacje, barberzy, usługi, salony, miasta) i wstawia zestaw demo od zera.
    /// Wywołuj tylko w Development i z włączoną flagą konfiguracji — patrz <c>DevDemoController</c>.
    /// </summary>
    public static async Task ReseedDemoDomainAsync(ApplicationDbContext db,
        CancellationToken cancellationToken = default)
    {
        await ClearDemoDomainAsync(db, cancellationToken);
        await InsertDemoDataAsync(db, cancellationToken);
    }

    private static async Task ClearDemoDomainAsync(ApplicationDbContext db,
        CancellationToken cancellationToken = default)
    {
        await db.Bookings.ExecuteDeleteAsync(cancellationToken);
        await db.Barbers.ExecuteDeleteAsync(cancellationToken);
        await db.Services.ExecuteDeleteAsync(cancellationToken);
        await db.Barbershops.ExecuteDeleteAsync(cancellationToken);
        await db.Cities.ExecuteDeleteAsync(cancellationToken);
    }

    private static async Task InsertDemoDataAsync(ApplicationDbContext db,
        CancellationToken cancellationToken = default)
    {
        var cityRows = new[]
        {
            ("Warszawa", 52.2297, 21.0122),
            ("Kraków", 50.0647, 19.9450),
            ("Wrocław", 51.1079, 17.0385),
            ("Rzeszów", 50.0412, 21.9990),
            ("Gdańsk", 54.3520, 18.6466),
            ("Poznań", 52.4064, 16.9252),
            ("Łódź", 51.7592, 19.4560),
            ("Katowice", 50.2649, 19.0238),
            ("Lublin", 51.2465, 22.5684),
        };

        var cities = cityRows.Select(c => new City
        {
            Name = c.Item1,
            CenterLatitude = c.Item2,
            CenterLongitude = c.Item3,
        }).ToList();

        db.Cities.AddRange(cities);
        await db.SaveChangesAsync(cancellationToken);

        var cityId = cities.ToDictionary(c => c.Name, c => c.Id, StringComparer.Ordinal);

        var shopDefs = new List<ShopSeed>
        {
            // Warszawa
            new("Warszawa", "1SECONDCUT Barbershop", "ul. Marszałkowska 85, 00-683 Warszawa",
                "Salon w centrum — strzyżenia męskie, broda, usługi dla dzieci.",
                "Sprawdź rezerwację na stronie salonu.", "Pn–Sb wg grafiku na stronie",
                52.2287, 21.0113),
            new("Warszawa", "Black Bear Barbershop", "ul. Jana Kazimierza, 01-267 Warszawa",
                "Strzyżenie, broda, golenie klasyczne, zabiegi pielęgnacyjne.",
                "+48 22 — kontakt na stronie", "Pn–Sb wg strony",
                52.2305, 20.9850),
            new("Warszawa", "Barbershop Wincentego", "ul. św. Wincentego 18, 03-126 Warszawa",
                "Targówek — klasyczne cięcia i pakiety combo.",
                "Telefon na stronie wizytówki", "Pn–Sb wg strony",
                52.2654, 21.0465),
            new("Warszawa", "Gentlemen Barber Shop — Mokotów", "ul. Jana III Sobieskiego 45, 02-930 Warszawa",
                "Sieć salonów — strzyżenie, broda, golenie brzytwą.",
                "Telefon na stronie gentlemenbarber.pl", "Pn–Sb wg strony",
                52.1857, 21.0934),
            new("Warszawa", "Razura Barber Shop", "ul. Polna 10/14, 00-629 Warszawa",
                "Śródmieście — strzyżenie, broda, golenie tradycyjne.",
                "Kontakt na stronie razura.pl", "Pn–Pt 9:00–18:00, Sb 9:00–16:00",
                52.2156, 21.0098),

            // Kraków
            new("Kraków", "iBarber", "ul. Krakowska 21, 31-062 Kraków",
                "Strzyżenie męskie, broda, rezerwacja online.",
                "+48 789 162 934", "Codziennie 8:00–22:00",
                50.0464, 19.9459),
            new("Kraków", "UOMO Barbershop", "ul. Garbarska 4, 31-131 Kraków",
                "Centrum — fryzjer męski, stylizacja.",
                "+48 577 779 494", "Pn–Pt 10:00–19:00, Sb 10:00–17:00",
                50.0628, 19.9360),
            new("Kraków", "SZTUKA BARBERSHOP — Floriańska", "ul. Floriańska 24, 31-019 Kraków",
                "Stare Miasto — strzyżenia i broda.",
                "Kontakt: sztukabarber.pl", "Pn–Sb wg strony",
                50.0617, 19.9405),
            new("Kraków", "Kruk Barbershop — Wrocławska", "ul. Wrocławska 60, 30-011 Kraków",
                "Sieć salonów w Krakowie.",
                "Kontakt: krukbarbershop.pl", "Pn–Sb wg strony",
                50.0768, 19.9382),
            new("Kraków", "Gentlemen Barber Shop — Kazimierz", "ul. Józefa Dietla 80/82, 31-073 Kraków",
                "Kazimierz — pełen zakres usług barberskich.",
                "+48 732 126 188", "Pn–Nd wg strony",
                50.0515, 19.9445),

            // Wrocław
            new("Wrocław", "BARBERSHOP CARTEL", "ul. Świętokrzyska 35b, 50-327 Wrocław",
                "Śródmieście — duża liczba opinii w katalogach rezerwacji.",
                "Rezerwacja np. Booksy", "Pn–Sb wg grafiku",
                51.1162, 17.0459),
            new("Wrocław", "Guziec Barber Shop", "ul. Nyska 61a, 50-505 Wrocław",
                "Strzyżenia męskie, broda.",
                "601 997 698", "Pn–Pt 9:00–20:00, Sb 8:00–15:00",
                51.1005, 17.0237),
            new("Wrocław", "Barber Academy — Św. Antoniego", "ul. Świętego Antoniego 31, 50-073 Wrocław",
                "Salon i szkolenia — fryzjerstwo męskie.",
                "500 190 403", "Pn–Pt 8:00–20:00, Sb 8:00–14:00",
                51.1108, 17.0338),
            new("Wrocław", "Barber Shop Papa", "ul. Kiełczowska 51B, 51-180 Wrocław",
                "Psie Pole — strzyżenie męskie.",
                "Kontakt: bspapa.pl", "Pn–Sb wg strony",
                51.1389, 17.0762),

            // Rzeszów
            new("Rzeszów", "Zacny Barber", "ul. Lwowska 6, 35-301 Rzeszów",
                "Strzyżenie, broda, pielęgnacja — dużo opinii klientów.",
                "+48 733 591 701", "Pn–Pt 9:00–20:00, Sb 9:00–15:00",
                50.0335, 22.0068),
            new("Rzeszów", "Gentlemen Barber Shop — Paderewskiego", "ul. Ignacego Paderewskiego 51C/U6, 35-330 Rzeszów",
                "Dzielnica Parkowa — sieć Gentlemen.",
                "+48 690 800 845", "Pn–Pt 8:00–21:00, Sb 8:00–15:00",
                50.0289, 22.0072),
            new("Rzeszów", "Black Blade Barber", "ul. Adama Mickiewicza 19, 35-065 Rzeszów",
                "Klimatyczny salon — strzyżenie i broda.",
                "+48 531 227 144", "Pn–Pt 10:00–21:00, Sb 9:00–15:00",
                50.0389, 22.0014),
            new("Rzeszów", "Sigma Barbershop", "ul. Graniczna 4b, 35-326 Rzeszów",
                "Salon barberski — rezerwacja online.",
                "+48 725 526 532", "Pn–Nd wg strony sigmabarbershop.pl",
                50.0183, 21.9839),

            // Gdańsk
            new("Gdańsk", "Barber Gdańsk — Szeroka", "ul. Szeroka 119/120, 80-835 Gdańsk",
                "Centrum — wejście od Świętojańskiej.",
                "Kontakt: barbergdansk.com", "Codziennie 10:00–20:00",
                54.3496, 18.6532),
            new("Gdańsk", "KOBALT BARBERSHOP", "ul. Kartuska 214, 80-125 Gdańsk",
                "Siedlce — parking przy salonie.",
                "Kontakt: kobaltbarbershop.com", "Pn–Sb wg strony",
                54.3662, 18.5934),
            new("Gdańsk", "Barber Shop — Garncarska", "ul. Garncarska 7/8/9, 80-835 Gdańsk",
                "Stare Miasto — fryzjer męski.",
                "Kontakt w katalogu miejsc Trójmiasta", "Pn–Pt 9:00–20:00, Sb 9:00–16:00",
                54.3499, 18.6550),
            new("Gdańsk", "Sztuka Cięcia Barber Shop", "al. Grunwaldzka 59, 80-254 Gdańsk",
                "Wrzeszcz — strzyżenia męskie.",
                "Kontakt w wizytówce trojmiasto.pl", "Pn–Sb wg strony",
                54.3768, 18.6058),

            // Poznań
            new("Poznań", "The Bro Barbershop", "ul. Studniarskiego 1/28, 61-021 Poznań",
                "Stylowy fryzjer męski.",
                "+48 696 824 275", "Pn–Pt 9:00–20:00, Sb 8:00–16:00",
                52.4058, 16.9284),
            new("Poznań", "SHO BarberShop", "ul. Głogowska 106, 60-266 Poznań",
                "Łazarz — strzyżenie męskie.",
                "Kontakt: shobarbershop.pl", "Pn–Sb wg strony",
                52.4068, 16.9001),
            new("Poznań", "Gentlemen Barber Shop — Śliska", "ul. Śliska 17/U3, 61-369 Poznań",
                "Sieć Gentlemen.",
                "+48 514 076 688", "Pn–Pt 8:00–20:00, Sb 8:00–16:00",
                52.3921, 16.9598),
            new("Poznań", "Gentlemen Barber Shop — Jeleniogórska", "ul. Jeleniogórska 14/1, 60-179 Poznań",
                "Druga lokalizacja sieci w Poznaniu.",
                "+48 609 260 172", "Pn–Pt 9:00–21:00, Sb 8:00–16:00",
                52.4331, 16.9084),
            new("Poznań", "CZAJA Barbershop", "ul. Szymańskiego 1, 61-826 Poznań",
                "Salon i akademia barberska.",
                "+48 669 747 997", "Pn–Pt 9:00–20:00, Sb 9:00–16:00",
                52.3789, 16.9653),

            // Łódź
            new("Łódź", "Kosa na Włosa", "ul. Piotrkowska 173, 90-447 Łódź",
                "Barber Shop przy Piotrkowskiej.",
                "+48 531 135 323", "Pn–Pt 10:00–20:00, Sb 9:00–15:00",
                51.7601, 19.4668),
            new("Łódź", "Brush Barber Shop", "ul. Piotrkowska 138/140, 90-062 Łódź",
                "Freedom Barber — lokal przy deptaku.",
                "795 770 577", "Pn–Sb wg strony",
                51.7582, 19.4679),
            new("Łódź", "Gentlemen Barber Shop — Składowa", "ul. Składowa 38/U1B, 90-127 Łódź",
                "Sieć Gentlemen.",
                "+48 886 515 959", "Pn–Pt 8:00–21:00, Sb 8:00–15:00",
                51.7822, 19.4714),
            new("Łódź", "DISTRICT13 BARBERSHOP", "ul. 6 Sierpnia 19A, 90-616 Łódź",
                "Polesie — rezerwacja online.",
                "Kontakt w katalogu Booksy", "Pn–Sb wg grafiku",
                51.7668, 19.4556),
            new("Łódź", "Gentlemen Barber Shop — Wspólna", "ul. Wspólna 1, 91-464 Łódź",
                "Kolejna lokalizacja sieci.",
                "+48 730 515 315", "Pn–Pt 8:00–21:00, Sb 8:00–15:00",
                51.8008, 19.4625),

            // Katowice
            new("Katowice", "Lumberjack Barber Shop", "al. Wojciecha Korfantego 2, 40-004 Katowice",
                "Centrum — strzyżenie i broda.",
                "514 553 222", "Pn–Pt 9:00–21:00, Sb 9:00–14:00",
                50.2597, 19.0152),
            new("Katowice", "CUTU Barbershop", "ul. Mikołowska 35, 40-065 Katowice",
                "Śródmieście — barber shop.",
                "Kontakt: cutu-barbershop.pl", "Pn–Sb wg strony",
                50.2446, 19.0088),
            new("Katowice", "The Barber's Knife — Bażantowo", "ul. Bażantów 6a/1a, 40-668 Katowice",
                "Berendowicz & Kublin — barbershop.",
                "+48 730 740 746", "Pn–Pt 9:00–20:00, Sb 9:00–17:00",
                50.1877, 19.0368),
            new("Katowice", "Brzytwa Mać Barber Shop", "ul. Ściegiennego 45, 40-001 Katowice",
                "Klasyczne strzyżenia męskie.",
                "+48 32 307 63 63", "Pn–Sb wg strony",
                50.2640, 19.0251),

            // Lublin
            new("Lublin", "Twenty One Barber Shop", "al. Warszawska 21a, 20-082 Lublin",
                "Strzyżenie męskie, broda.",
                "+48 605 033 198", "Pn–Pt 10:00–19:00, Sb 10:00–16:00",
                51.2256, 22.5297),
            new("Lublin", "Gentlemen Barber Shop — Koralowa", "ul. Koralowa 33, 20-583 Lublin",
                "Sieć Gentlemen.",
                "+48 796 001 883", "Pn–Pt 8:00–21:00, Sb 8:00–15:00",
                51.2389, 22.5198),
            new("Lublin", "Dystrykt Dziesiąty Barbershop", "ul. Władysława Kunickiego 51, 20-412 Lublin",
                "Rezerwacja online — wysoka ocena w katalogu.",
                "Kontakt w Booksy", "Pn–Sb wg grafiku",
                51.2327, 22.4978),
            new("Lublin", "Barber Schron", "ul. Orla 8, 20-022 Lublin",
                "Mały salon w centrum.",
                "+48 733 110 120", "Pn–Sb wg strony",
                51.2480, 22.5660),
        };

        var shops = shopDefs.Select(def => new Barbershop
        {
            CityId = cityId[def.CityName],
            Name = def.Name,
            Description = def.Description,
            Address = def.Address,
            Phone = def.Phone,
            OpeningHoursNotes = def.Hours,
            Latitude = def.Latitude,
            Longitude = def.Longitude,
            DataSource = BarbershopDataSource.Seed,
        }).ToList();

        db.Barbershops.AddRange(shops);
        await db.SaveChangesAsync(cancellationToken);

        var barbers = new List<Barber>();
        var services = new List<Service>();

        var barberFirstNames = new[]
        {
            "Michał", "Tomek", "Kuba", "Piotr", "Adam", "Bartek", "Olek", "Marek", "Kamil", "Dawid",
            "Maciej", "Łukasz", "Jan", "Wojtek", "Sebastian",
        };

        var standardServices = new (string Name, int Minutes, decimal Price)[]
        {
            ("Strzyżenie męskie", 30, 80m),
            ("Strzyżenie + broda", 45, 110m),
            ("Golenie brzytwą", 30, 90m),
            ("Kontur / fade", 45, 100m),
        };

        var i = 0;
        foreach (var shop in shops)
        {
            var a = barberFirstNames[i % barberFirstNames.Length];
            var b = barberFirstNames[(i + 7) % barberFirstNames.Length];
            barbers.Add(new Barber { BarbershopId = shop.Id, DisplayName = $"{a} K." });
            barbers.Add(new Barber { BarbershopId = shop.Id, DisplayName = $"{b} W." });
            foreach (var s in standardServices)
            {
                services.Add(new Service
                {
                    BarbershopId = shop.Id,
                    Name = s.Name,
                    DurationMinutes = s.Minutes,
                    Price = s.Price,
                });
            }

            i++;
        }

        db.Barbers.AddRange(barbers);
        db.Services.AddRange(services);
        await db.SaveChangesAsync(cancellationToken);
    }

    private sealed record ShopSeed(
        string CityName,
        string Name,
        string Address,
        string Description,
        string Phone,
        string Hours,
        double Latitude,
        double Longitude);
}
