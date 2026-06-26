using GestionSpa.Api.Data;
using GestionSpa.Api.Services;
using Microsoft.EntityFrameworkCore;

static string GetConnectionString(IConfiguration config)
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrEmpty(databaseUrl))
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var user = Uri.UnescapeDataString(userInfo[0]);
        var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var db = uri.AbsolutePath.TrimStart('/');
        return $"Host={uri.Host};Port={uri.Port};Database={db};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true";
    }
    return config.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("No hay cadena de conexión configurada");
}

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(GetConnectionString(builder.Configuration)));

builder.Services.AddScoped<CuotaService>();

static string[] GetCorsOrigins()
{
    var origins = new List<string>();
    foreach (var key in new[] { "FRONTEND_URL", "CORS_ORIGINS" })
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value)) continue;
        origins.AddRange(value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
    if (origins.Count == 0)
        origins.Add("http://localhost:5173");
    return origins.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
}

var corsOrigins = GetCorsOrigins();
var allowCloudflarePages = string.Equals(
    Environment.GetEnvironmentVariable("CORS_ALLOW_PAGES_DEV"), "true", StringComparison.OrdinalIgnoreCase);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (corsOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                return true;
            if (allowCloudflarePages &&
                Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                uri.Scheme == "https" &&
                uri.Host.EndsWith(".pages.dev", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        })
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DbSeeder.SeedAsync(db);
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");
app.MapControllers();

app.Run();
