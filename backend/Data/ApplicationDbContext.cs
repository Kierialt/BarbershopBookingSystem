using backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<City> Cities => Set<City>();
    public DbSet<Barbershop> Barbershops => Set<Barbershop>();
    public DbSet<Barber> Barbers => Set<Barber>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Barbershop>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Address).HasMaxLength(400);
            e.Property(x => x.Phone).HasMaxLength(40);
            e.Property(x => x.ExternalPlaceId).HasMaxLength(256);
            e.HasIndex(x => x.ExternalPlaceId);
            e.HasOne(x => x.City).WithMany(x => x.Barbershops).HasForeignKey(x => x.CityId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Owner).WithMany(x => x.OwnedBarbershops).HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<City>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(120);
            e.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<Barber>(e =>
        {
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.HasOne(x => x.Barbershop).WithMany(x => x.Barbers).HasForeignKey(x => x.BarbershopId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ApplicationUser).WithMany(x => x.BarberProfiles).HasForeignKey(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Service>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Currency).HasMaxLength(8);
            e.Property(x => x.Price).HasPrecision(10, 2);
            e.HasOne(x => x.Barbershop).WithMany(x => x.Services).HasForeignKey(x => x.BarbershopId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Booking>(e =>
        {
            e.HasOne(x => x.Client).WithMany(x => x.ClientBookings).HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Barber).WithMany(x => x.Bookings).HasForeignKey(x => x.BarberId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Service).WithMany(x => x.Bookings).HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.BarberId, x.StartUtc });
        });

        modelBuilder.Entity<Review>(e =>
        {
            e.Property(x => x.Comment).HasMaxLength(2000);
            e.HasOne(x => x.Barbershop).WithMany(x => x.Reviews).HasForeignKey(x => x.BarbershopId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Client).WithMany(x => x.AuthoredReviews).HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.BarbershopId);
            e.HasIndex(x => x.ModerationStatus);
        });
    }
}
