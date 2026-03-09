using System.Text.Json;
using BlutdruckErfassungApp.Components;
using BlutdruckErfassungApp.Data;
using BlutdruckErfassungApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

await AddLocalVaultSecretsAsync(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = "Server=(localdb)\\mssqllocaldb;Database=BlutdruckErfassungApp;Trusted_Connection=True;TrustServerCertificate=True;";
}

var forceSecureCookies = builder.Configuration.GetValue("Security:ForceSecureCookies", !builder.Environment.IsDevelopment());

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRazorPages();
builder.Services.AddHttpClient();

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IOcrService, AzureDocumentIntelligenceOcrService>();
builder.Services.AddSingleton<OcrParsingService>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
        options.Cookie.Name = "bp-auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = forceSecureCookies
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
    });

builder.Services.AddAuthorization();

var keyRingPath = builder.Configuration["Security:DataProtectionPath"]
    ?? (builder.Environment.IsDevelopment() ? "./keyring-dev" : "/app/keyring");
Directory.CreateDirectory(keyRingPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
    options.RequireHeaderSymmetry = false;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseForwardedHeaders();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorPages();

app.MapPost("/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/Login");
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await using (var scope = app.Services.CreateAsyncScope())
{
    var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
    await authService.EnsureDatabaseAsync();
    await authService.EnsureBootstrapAdminAsync();
}

app.Run();

static async Task AddLocalVaultSecretsAsync(ConfigurationManager configuration)
{
    var localVaultEnabled = configuration.GetValue("LocalVault:Enabled", false);
    if (!localVaultEnabled)
    {
        return;
    }

    var address = configuration["LocalVault:Address"]?.TrimEnd('/');
    var token = configuration["LocalVault:Token"];
    var mount = configuration["LocalVault:Mount"] ?? "secret";
    var secretPath = configuration["LocalVault:SecretPath"] ?? "blutdruck";

    if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(token))
    {
        throw new InvalidOperationException("LocalVault ist aktiviert, aber Address oder Token fehlen.");
    }

    var requestUri = $"{address}/v1/{mount.Trim('/')}/data/{secretPath.Trim('/')}";

    using var httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
    request.Headers.Add("X-Vault-Token", token);

    using var response = await httpClient.SendAsync(request);
    if (!response.IsSuccessStatusCode)
    {
        var responseBody = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"Lokaler Vault konnte nicht gelesen werden. HTTP {(int)response.StatusCode}: {responseBody}");
    }

    await using var responseStream = await response.Content.ReadAsStreamAsync();
    using var json = await JsonDocument.ParseAsync(responseStream);

    if (!json.RootElement.TryGetProperty("data", out var dataNode) ||
        !dataNode.TryGetProperty("data", out var secretsNode))
    {
        throw new InvalidOperationException("Ungültige Vault-Antwort: erwartete Struktur data.data fehlt.");
    }

    var flattenedSecrets = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    foreach (var property in secretsNode.EnumerateObject())
    {
        var key = property.Name.Replace("--", ":", StringComparison.Ordinal);
        var value = property.Value.ValueKind == JsonValueKind.String
            ? property.Value.GetString()
            : property.Value.ToString();

        flattenedSecrets[key] = value;
    }

    configuration.AddInMemoryCollection(flattenedSecrets);
}
