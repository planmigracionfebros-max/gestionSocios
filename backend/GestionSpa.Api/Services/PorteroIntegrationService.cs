using System.Text;
using System.Text.Json;
using GestionSpa.Api.Data;
using GestionSpa.Api.DTOs;
using GestionSpa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionSpa.Api.Services;

public interface IPorteroIntegrationService
{
    Task<EmisorPorteroConfig?> GetConfigAsync(int emisorId);
    Task<PorteroConfigDto> GetConfigDtoAsync(int emisorId, string emisorSlug, string? apiPublicBaseUrl);
    Task<PorteroConfigDto> SaveConfigAsync(int emisorId, string emisorSlug, GuardarPorteroConfigDto dto, string? apiPublicBaseUrl);
    Task<PorteroPruebaConexionDto> TestConnectionAsync(EmisorPorteroConfig config);
    Task<PorteroSincronizacionDto> SyncAllActiveSociosAsync(int emisorId);
    Task TrySyncSocioAsync(Socio socio);
    Task TryRemoveSocioAsync(Socio socio);
    Task ProcessAccessWebhookAsync(string emisorSlug, PorteroWebhookPayload payload);
    Task<PorteroAccionDto> AbrirPuertaAsync(EmisorPorteroConfig config);
}

public class PorteroWebhookPayload
{
    public string Event { get; set; } = "";
    public JsonElement Data { get; set; }
}

public class PorteroIntegrationService(
    AppDbContext db,
    IHttpClientFactory httpClientFactory,
    ILogger<PorteroIntegrationService> logger) : IPorteroIntegrationService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<EmisorPorteroConfig?> GetConfigAsync(int emisorId) =>
        await db.EmisorPorteroConfigs.FirstOrDefaultAsync(c => c.EmisorId == emisorId);

    public async Task<PorteroConfigDto> GetConfigDtoAsync(int emisorId, string emisorSlug, string? apiPublicBaseUrl)
    {
        var config = await GetConfigAsync(emisorId);
        return MapConfigDto(config, emisorSlug, apiPublicBaseUrl);
    }

    public async Task<PorteroConfigDto> SaveConfigAsync(int emisorId, string emisorSlug, GuardarPorteroConfigDto dto, string? apiPublicBaseUrl)
    {
        var config = await db.EmisorPorteroConfigs.FirstOrDefaultAsync(c => c.EmisorId == emisorId);
        if (config is null)
        {
            config = new EmisorPorteroConfig { EmisorId = emisorId };
            db.EmisorPorteroConfigs.Add(config);
        }

        config.Habilitado = dto.Habilitado;
        config.ApiUrl = dto.ApiUrl.Trim().TrimEnd('/');
        config.ApiKey = dto.ApiKey.Trim();
        config.WebhookSecret = string.IsNullOrWhiteSpace(dto.WebhookSecret) ? null : dto.WebhookSecret.Trim();
        config.DeviceSn = string.IsNullOrWhiteSpace(dto.DeviceSn) ? "7674222960189" : dto.DeviceSn.Trim();
        config.SincronizarAutomatico = dto.SincronizarAutomatico;
        config.FechaActualizacion = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return MapConfigDto(config, emisorSlug, apiPublicBaseUrl);
    }

    public async Task<PorteroPruebaConexionDto> TestConnectionAsync(EmisorPorteroConfig config)
    {
        try
        {
            var health = await SendAsync<JsonElement>(config, HttpMethod.Get, "/api/health", auth: false);
            var stats = await SendAsync<JsonElement>(config, HttpMethod.Get, "/api/stats");
            var service = health.TryGetProperty("service", out var s) ? s.GetString() : "ApiPorteroSpa";
            return new PorteroPruebaConexionDto(true, $"Conexión OK con {service}", stats);
        }
        catch (Exception ex)
        {
            return new PorteroPruebaConexionDto(false, ex.Message, null);
        }
    }

    public async Task<PorteroSincronizacionDto> SyncAllActiveSociosAsync(int emisorId)
    {
        var config = await GetConfigAsync(emisorId);
        if (config is null || !config.Habilitado)
            return new PorteroSincronizacionDto(0, 0, 0, ["Portero no habilitado para este emisor"]);

        var socios = await db.Socios.Where(s => s.EmisorId == emisorId && s.Estado == EstadoSocio.Activo).ToListAsync();
        var errores = new List<string>();
        var ok = 0;
        foreach (var socio in socios)
        {
            try
            {
                await SyncSocioInternalAsync(socio, config);
                ok++;
            }
            catch (Exception ex)
            {
                errores.Add($"{socio.NumeroSocio} {socio.Nombre}: {ex.Message}");
            }
        }
        return new PorteroSincronizacionDto(socios.Count, ok, socios.Count - ok, errores);
    }

    public async Task TrySyncSocioAsync(Socio socio)
    {
        var config = await GetConfigAsync(socio.EmisorId);
        if (config is null || !config.Habilitado || !config.SincronizarAutomatico) return;
        if (socio.Estado != EstadoSocio.Activo) return;

        try
        {
            await SyncSocioInternalAsync(socio, config);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error sincronizando socio {SocioId} con portero", socio.Id);
        }
    }

    public async Task TryRemoveSocioAsync(Socio socio)
    {
        var config = await GetConfigAsync(socio.EmisorId);
        if (config is null || !config.Habilitado || !config.SincronizarAutomatico) return;

        try
        {
            var pin = ToPorteroPin(socio);
            await SendAsync<JsonElement>(config, HttpMethod.Delete, $"/api/socios/{Uri.EscapeDataString(pin)}",
                body: new { device_sn = config.DeviceSn });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error dando de baja socio {SocioId} en portero", socio.Id);
        }
    }

    public async Task ProcessAccessWebhookAsync(string emisorSlug, PorteroWebhookPayload payload)
    {
        var emisor = await db.Emisores.FirstOrDefaultAsync(e => e.Slug == emisorSlug && e.Activo);
        if (emisor is null) throw new InvalidOperationException("Emisor no encontrado");

        if (payload.Event != "access") return;

        var pin = GetString(payload.Data, "pin");
        if (string.IsNullOrEmpty(pin)) return;

        var socio = await FindSocioByPinAsync(emisor.Id, pin);
        if (socio is null)
        {
            logger.LogWarning("Webhook access: socio no encontrado pin={Pin} emisor={Slug}", pin, emisorSlug);
            return;
        }

        var accessType = GetString(payload.Data, "access_type") ?? "entry";
        var tipo = accessType.Equals("exit", StringComparison.OrdinalIgnoreCase) ? TipoIngreso.Salida : TipoIngreso.Entrada;
        var timestamp = GetString(payload.Data, "timestamp");
        var fecha = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(timestamp) && DateTime.TryParse(timestamp, out var parsed))
            fecha = parsed.ToUniversalTime();

        db.Ingresos.Add(new Ingreso
        {
            EmisorId = emisor.Id,
            SocioId = socio.Id,
            FechaHora = fecha,
            Tipo = tipo,
            AccesoPermitido = true,
            MotivoRechazo = $"Portero ({GetString(payload.Data, "method") ?? "biométrico"})",
        });
        await db.SaveChangesAsync();
    }

    public async Task<PorteroAccionDto> AbrirPuertaAsync(EmisorPorteroConfig config)
    {
        var result = await SendAsync<JsonElement>(config, HttpMethod.Post,
            $"/api/dispositivos/{Uri.EscapeDataString(config.DeviceSn)}/abrir-puerta");
        return new PorteroAccionDto("Comando de apertura encolado en el portero (~10 s)", result);
    }

    private async Task SyncSocioInternalAsync(Socio socio, EmisorPorteroConfig config)
    {
        var pin = ToPorteroPin(socio);
        var nombre = $"{socio.Nombre} {socio.Apellido}".Trim();
        var validDays = socio.FechaVencimiento.HasValue
            ? Math.Max(1, (int)(socio.FechaVencimiento.Value.Date - DateTime.UtcNow.Date).TotalDays)
            : 365;

        await SendAsync<JsonElement>(config, HttpMethod.Post, "/api/socios", new
        {
            nombre,
            cedula = pin,
            celular = socio.Telefono ?? "",
            email = socio.Email ?? "",
            membership_type = "socio",
            access_level = 1,
            valid_days = validDays,
            device_sn = config.DeviceSn,
        });
    }

    private async Task<Socio?> FindSocioByPinAsync(int emisorId, string pin)
    {
        var socios = await db.Socios.Where(s => s.EmisorId == emisorId).ToListAsync();
        return socios.FirstOrDefault(s =>
            ToPorteroPin(s) == pin ||
            new string(s.Cedula.Where(char.IsDigit).ToArray()) == pin ||
            s.NumeroSocio == pin);
    }

    public static string ToPorteroPin(Socio socio)
    {
        var digits = new string(socio.Cedula.Where(char.IsDigit).ToArray());
        if (digits.Length > 0)
            return digits.Length > 9 ? digits[^9..] : digits;
        return socio.NumeroSocio;
    }

    private async Task<T> SendAsync<T>(EmisorPorteroConfig config, HttpMethod method, string path, object? body = null, bool auth = true)
    {
        var client = httpClientFactory.CreateClient("Portero");
        var url = $"{config.ApiUrl.TrimEnd('/')}{path}";
        using var request = new HttpRequestMessage(method, url);
        if (auth)
            request.Headers.Add("X-API-Key", config.ApiKey);

        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json");
        }

        var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"ApiPortero {((int)response.StatusCode)}: {text}");

        if (string.IsNullOrWhiteSpace(text))
            return default!;

        if (typeof(T) == typeof(JsonElement))
            return (T)(object)JsonSerializer.Deserialize<JsonElement>(text);

        return JsonSerializer.Deserialize<T>(text, JsonOpts)
            ?? throw new InvalidOperationException("Respuesta vacía de ApiPortero");
    }

    private static string? GetString(JsonElement data, string name) =>
        data.TryGetProperty(name, out var prop) ? prop.GetString() : null;

    private static PorteroConfigDto MapConfigDto(EmisorPorteroConfig? config, string emisorSlug, string? apiPublicBaseUrl)
    {
        var baseUrl = (apiPublicBaseUrl ?? "").TrimEnd('/');
        var webhookUrl = string.IsNullOrEmpty(baseUrl)
            ? $"/api/webhooks/portero/{emisorSlug}"
            : $"{baseUrl}/api/webhooks/portero/{emisorSlug}";

        if (config is null)
            return new PorteroConfigDto(false, "http://localhost:5000", "", null, "7674222960189", true, webhookUrl, null);

        return new PorteroConfigDto(
            config.Habilitado,
            config.ApiUrl,
            config.ApiKey,
            config.WebhookSecret,
            config.DeviceSn,
            config.SincronizarAutomatico,
            webhookUrl,
            config.FechaActualizacion);
    }
}
