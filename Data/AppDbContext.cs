using Microsoft.EntityFrameworkCore;

namespace BlutdruckErfassungApp.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<BloodPressureReading> BloodPressureReadings => Set<BloodPressureReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>()
            .HasIndex(x => x.UserNameNormalized)
            .IsUnique();

        modelBuilder.Entity<BloodPressureReading>()
            .HasIndex(x => new { x.UserNameNormalized, x.MeasuredAtUtc });
    }
}
