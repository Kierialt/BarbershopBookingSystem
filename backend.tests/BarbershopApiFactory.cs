using backend.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace backend.tests;

/// <summary>
/// Host testowy z SQLite w pamięci (jedno otwarte połączenie), stałym kluczem JWT i środowiskiem Testing
/// (bez seedowania kont dev z appsettings).
/// </summary>
public sealed class BarbershopApiFactory : WebApplicationFactory<Program>, IDisposable
{
    private SqliteConnection? _sqliteConnection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // WebApplicationBuilder odczytuje Jwt zanim zadziała ConfigureAppConfiguration — UseSetting trafia do konfiguracji hosta na czas budowy.
        builder.UseSetting("Jwt:Key", "SuperSecretTestKeyForJwtMustBeLongEnough32");
        builder.UseSetting("Jwt:Issuer", "BarbershopApiTest");
        builder.UseSetting("Jwt:Audience", "BarbershopClientTest");
        builder.UseSetting("Jwt:ExpireMinutes", "120");
        builder.UseSetting("Cors:Origins:0", "http://localhost");

        builder.ConfigureServices(services =>
        {
            _sqliteConnection = new SqliteConnection("DataSource=:memory:");
            _sqliteConnection.Open();

            RemoveDbContextRegistration(services);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(_sqliteConnection));
        });
    }

    private static void RemoveDbContextRegistration(IServiceCollection services)
    {
        var toRemove = services.Where(d =>
            d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
            || d.ServiceType == typeof(ApplicationDbContext)).ToList();

        foreach (var d in toRemove)
            services.Remove(d);
    }

    public new void Dispose()
    {
        base.Dispose();
        _sqliteConnection?.Dispose();
    }
}
