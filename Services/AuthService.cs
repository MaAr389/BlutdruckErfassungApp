using BlutdruckErfassungApp.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlutdruckErfassungApp.Services;

public sealed class AuthService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    IPasswordHasher<AppUser> passwordHasher,
    IConfiguration configuration,
    ILogger<AuthService> logger)
{
    public async Task EnsureDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await db.Database.EnsureCreatedAsync(cancellationToken);
    }

    public async Task EnsureBootstrapAdminAsync(CancellationToken cancellationToken = default)
    {
        var bootstrapUserName = configuration["BootstrapAdmin:Username"];
        var bootstrapPassword = configuration["BootstrapAdmin:Password"];

        if (string.IsNullOrWhiteSpace(bootstrapUserName) || string.IsNullOrWhiteSpace(bootstrapPassword))
        {
            logger.LogWarning("Kein Bootstrap-Admin gesetzt. Login ist erst möglich, wenn BootstrapAdmin konfiguriert ist.");
            return;
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var normalizedUserName = NormalizeUserName(bootstrapUserName);

        var existing = await db.Users
            .SingleOrDefaultAsync(x => x.UserNameNormalized == normalizedUserName, cancellationToken);

        if (existing is not null)
        {
            return;
        }

        var newUser = new AppUser
        {
            UserName = bootstrapUserName,
            UserNameNormalized = normalizedUserName
        };

        newUser.PasswordHash = passwordHasher.HashPassword(newUser, bootstrapPassword);

        db.Users.Add(newUser);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Bootstrap-Admin wurde erstellt: {UserName}", bootstrapUserName);
    }

    public async Task<AppUser?> ValidateCredentialsAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var normalizedUserName = NormalizeUserName(userName);

        var user = await db.Users
            .SingleOrDefaultAsync(x => x.UserNameNormalized == normalizedUserName && x.IsActive, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return verification is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded
            ? user
            : null;
    }

    public static string NormalizeUserName(string userName) => userName.Trim().ToUpperInvariant();
}
