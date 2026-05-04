using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public static class BarbershopDemoSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Cities.AnyAsync(cancellationToken))
            return;

        var cities = new[]
        {
            new City { Name = "Warszawa", CenterLatitude = 52.2297, CenterLongitude = 21.0122 },
            new City { Name = "Kraków", CenterLatitude = 50.0647, CenterLongitude = 19.9450 },
            new City { Name = "Wrocław", CenterLatitude = 51.1079, CenterLongitude = 17.0385 },
        };
        db.Cities.AddRange(cities);
        await db.SaveChangesAsync(cancellationToken);

        var waw = cities[0];
        var krk = cities[1];
        var wro = cities[2];

        var shops = new List<Barbershop>
        {
            new()
            {
                CityId = waw.Id,
                Name = "Brother’s Cut — Śródmieście",
                Description = "Klasyczne strzyżenia męskie, broda i kontouring. Atmosfera vintage.",
                Address = "ul. Marszałkowska 45, 00-001 Warszawa",
                Phone = "+48 22 000 11 22",
                OpeningHoursNotes = "Pn–Sb 9:00–18:00, Nd nieczynne",
                Latitude = 52.2310,
                Longitude = 21.0115,
                DataSource = BarbershopDataSource.Seed,
            },
            new()
            {
                CityId = waw.Id,
                Name = "Fade Room Mokotów",
                Description = "Nowoczesne fade, design linii i koloryzacja.",
                Address = "ul. Puławska 120, 02-620 Warszawa",
                Phone = "+48 22 000 33 44",
                OpeningHoursNotes = "Pn–Sb 9:00–18:00",
                Latitude = 52.1825,
                Longitude = 21.0221,
                DataSource = BarbershopDataSource.Seed,
            },
            new()
            {
                CityId = krk.Id,
                Name = "Stary Kleparz Barber",
                Description = "Blisko targu, szybkie strzyżenia i golenie brzytwą.",
                Address = "ul. Basztowa 10, 31-143 Kraków",
                Phone = "+48 12 000 55 66",
                OpeningHoursNotes = "Pn–Sb 9:00–18:00",
                Latitude = 50.0650,
                Longitude = 19.9420,
                DataSource = BarbershopDataSource.Seed,
            },
            new()
            {
                CityId = wro.Id,
                Name = "Rynek 12 Barbershop",
                Description = "Widok na rynek, umów się online — bez kolejki.",
                Address = "Rynek 12, 50-101 Wrocław",
                Phone = "+48 71 000 77 88",
                OpeningHoursNotes = "Pn–Sb 9:00–18:00",
                Latitude = 51.1100,
                Longitude = 17.0320,
                DataSource = BarbershopDataSource.Seed,
            },
        };
        db.Barbershops.AddRange(shops);
        await db.SaveChangesAsync(cancellationToken);

        var barbers = new List<Barber>();
        var services = new List<Service>();

        void AddTeamAndServices(Barbershop shop, string[] names, (string Name, int Min, decimal Price)[] svc)
        {
            foreach (var n in names)
                barbers.Add(new Barber { BarbershopId = shop.Id, DisplayName = n });
            foreach (var s in svc)
                services.Add(new Service
                {
                    BarbershopId = shop.Id,
                    Name = s.Name,
                    DurationMinutes = s.Min,
                    Price = s.Price,
                });
        }

        var standardServices = new[]
        {
            ("Strzyżenie męskie", 30, 80m),
            ("Strzyżenie + broda", 45, 110m),
            ("Golenie brzytwą", 30, 90m),
            ("Kontur / fade", 45, 100m),
        };

        AddTeamAndServices(shops[0], new[] { "Michał K.", "Tomek W." }, standardServices);
        AddTeamAndServices(shops[1], new[] { "Kuba Z.", "Olek R.", "Marek P." }, standardServices);
        AddTeamAndServices(shops[2], new[] { "Piotr S.", "Adam L." }, standardServices);
        AddTeamAndServices(shops[3], new[] { "Bartek N." }, standardServices);

        db.Barbers.AddRange(barbers);
        db.Services.AddRange(services);
        await db.SaveChangesAsync(cancellationToken);
    }
}
