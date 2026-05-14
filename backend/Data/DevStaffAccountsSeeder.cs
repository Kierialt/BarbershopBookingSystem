using backend.Infrastructure;
using backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace backend.Data;

/// <summary>
/// W Development: konta demo (klient, właściciele pierwszych salonów, barberzy z loginem imię+nazwisko.id@domena),
/// przypisanie właścicieli do salonów, powiązanie barberów z użytkownikami, przykładowe opinie do moderacji.
/// </summary>
public static class DevStaffAccountsSeeder
{
    public static async Task EnsureAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue("Demo:SeedStaffAccounts", true))
            return;

        var domain = (configuration["Demo:StaffEmailDomain"] ?? "dev.barbershop.local").Trim();
        var barberPassword = configuration["Demo:DevBarberPassword"] ?? "BarberDev1!";
        var ownerPassword = configuration["Demo:DevOwnerPassword"] ?? "OwnerDev1!";
        var clientPassword = configuration["Demo:DevClientPassword"] ?? "KlientDev1!";

        await EnsureDemoClientAsync(db, userManager, domain, clientPassword, logger, cancellationToken);

        var shops = await db.Barbershops.OrderBy(s => s.Id).Take(3).ToListAsync(cancellationToken);
        foreach (var shop in shops)
        {
            var email = $"wlasciciel.salon{shop.Id}@{domain}";
            var owner = await userManager.FindByEmailAsync(email);
            if (owner is null)
            {
                owner = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = $"Właściciel — {shop.Name}",
                    EmailConfirmed = true,
                };
                var result = await userManager.CreateAsync(owner, ownerPassword);
                if (!result.Succeeded)
                {
                    logger?.LogWarning("Nie utworzono właściciela {Email}: {Errors}", email,
                        string.Join("; ", result.Errors.Select(e => e.Description)));
                    continue;
                }

                await userManager.AddToRoleAsync(owner, RoleNames.Wlasciciel);
            }
            else if (!await userManager.IsInRoleAsync(owner, RoleNames.Wlasciciel))
                await userManager.AddToRoleAsync(owner, RoleNames.Wlasciciel);

            shop.OwnerUserId = owner.Id;
        }

        await db.SaveChangesAsync(cancellationToken);

        var barbers = await db.Barbers.Where(b => b.ApplicationUserId == null).ToListAsync(cancellationToken);
        foreach (var barber in barbers)
            await LinkOrCreateBarberUserAsync(userManager, barber, domain, barberPassword, logger);

        await db.SaveChangesAsync(cancellationToken);

        await SeedDemoReviewsIfEmptyAsync(db, userManager, domain, logger, cancellationToken);
    }

    /// <summary>Po dodaniu barbera w panelu właściciela (Development) — utworzenie konta e-mail w stylu imię+nazwisko.id.</summary>
    public static async Task EnsureBarberLoginForSingleAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        int barberId,
        IConfiguration configuration,
        ILogger? logger,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue("Demo:SeedStaffAccounts", true))
            return;

        var barber = await db.Barbers.FirstOrDefaultAsync(b => b.Id == barberId, cancellationToken);
        if (barber is null || barber.ApplicationUserId is not null)
            return;

        var domain = (configuration["Demo:StaffEmailDomain"] ?? "dev.barbershop.local").Trim();
        var barberPassword = configuration["Demo:DevBarberPassword"] ?? "BarberDev1!";
        await LinkOrCreateBarberUserAsync(userManager, barber, domain, barberPassword, logger);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task LinkOrCreateBarberUserAsync(
        UserManager<ApplicationUser> userManager,
        Barber barber,
        string domain,
        string barberPassword,
        ILogger? logger)
    {
        var local = EmailSlug.BarberLoginLocalPart(barber.DisplayName, barber.Id);
        var email = $"{local}@{domain}";
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            barber.ApplicationUserId = user.Id;
            if (!await userManager.IsInRoleAsync(user, RoleNames.Barber))
                await userManager.AddToRoleAsync(user, RoleNames.Barber);
            return;
        }

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = barber.DisplayName,
            EmailConfirmed = true,
        };
        var create = await userManager.CreateAsync(user, barberPassword);
        if (!create.Succeeded)
        {
            logger?.LogWarning("Nie utworzono konta barbera {Email}: {Errors}", email,
                string.Join("; ", create.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, RoleNames.Barber);
        barber.ApplicationUserId = user.Id;
    }

    private static async Task EnsureDemoClientAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        string domain,
        string password,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        var email = $"klient.demo@{domain}";
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = "Klient demo",
            EmailConfirmed = true,
        };
        var r = await userManager.CreateAsync(user, password);
        if (!r.Succeeded)
        {
            logger?.LogWarning("Nie utworzono klienta demo: {Errors}",
                string.Join("; ", r.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, RoleNames.Klient);
    }

    private static async Task SeedDemoReviewsIfEmptyAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        string domain,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        if (await db.Reviews.AnyAsync(cancellationToken))
            return;

        var clientEmail = $"klient.demo@{domain}";
        var client = await userManager.FindByEmailAsync(clientEmail);
        if (client is null)
            return;

        var shops = await db.Barbershops.OrderBy(s => s.Id).Take(2).ToListAsync(cancellationToken);
        if (shops.Count == 0)
            return;

        db.Reviews.Add(new Review
        {
            BarbershopId = shops[0].Id,
            ClientId = client.Id,
            Rating = 5,
            Comment = "Świetna obsługa — czeka na akceptację administratora.",
            ModerationStatus = ReviewModerationStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
        });

        if (shops.Count > 1)
        {
            db.Reviews.Add(new Review
            {
                BarbershopId = shops[1].Id,
                ClientId = client.Id,
                Rating = 4,
                Comment = "Dobry klimat, drobne opóźnienie. Do moderacji.",
                ModerationStatus = ReviewModerationStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
