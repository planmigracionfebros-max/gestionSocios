using GestionSpa.Api.DTOs;
using GestionSpa.Api.Models;
using GestionSpa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionSpa.Api.Data;

namespace GestionSpa.Api.Controllers;

[ApiController]
[Route("api/portero")]
[Authorize(Roles = nameof(RolUsuario.AdminEmisor))]
public class PorteroController(
    AppDbContext db,
    ITenantContext tenant,
    IPorteroIntegrationService portero) : ControllerBase
{
    [HttpGet("config")]
    public async Task<ActionResult<PorteroConfigDto>> GetConfig()
    {
        var emisor = await RequireEmisorAsync();
        if (emisor is null) return Forbid();

        return await portero.GetConfigDtoAsync(emisor.Id, emisor.Slug, GetApiPublicBaseUrl());
    }

    [HttpPut("config")]
    public async Task<ActionResult<PorteroConfigDto>> SaveConfig(GuardarPorteroConfigDto dto)
    {
        var emisor = await RequireEmisorAsync();
        if (emisor is null) return Forbid();

        if (string.IsNullOrWhiteSpace(dto.ApiUrl))
            return BadRequest(new { mensaje = "La URL de ApiPortero es obligatoria", errores = new[] { "La URL de ApiPortero es obligatoria" } });
        if (dto.Habilitado && string.IsNullOrWhiteSpace(dto.ApiKey))
            return BadRequest(new { mensaje = "La API Key es obligatoria cuando el portero está habilitado", errores = new[] { "La API Key es obligatoria" } });

        return await portero.SaveConfigAsync(emisor.Id, emisor.Slug, dto, GetApiPublicBaseUrl());
    }

    [HttpPost("probar")]
    public async Task<ActionResult<PorteroPruebaConexionDto>> ProbarConexion([FromBody] GuardarPorteroConfigDto? dto)
    {
        var emisor = await RequireEmisorAsync();
        if (emisor is null) return Forbid();

        EmisorPorteroConfig config;
        if (dto is not null && !string.IsNullOrWhiteSpace(dto.ApiUrl))
        {
            config = new EmisorPorteroConfig
            {
                EmisorId = emisor.Id,
                ApiUrl = dto.ApiUrl.Trim().TrimEnd('/'),
                ApiKey = dto.ApiKey.Trim(),
                DeviceSn = string.IsNullOrWhiteSpace(dto.DeviceSn) ? "7674222960189" : dto.DeviceSn.Trim(),
            };
        }
        else
        {
            var saved = await portero.GetConfigAsync(emisor.Id);
            if (saved is null)
                return BadRequest(new { mensaje = "Guardá la configuración antes de probar", errores = new[] { "Configuración no guardada" } });
            config = saved;
        }

        return await portero.TestConnectionAsync(config);
    }

    [HttpPost("sincronizar")]
    public async Task<ActionResult<PorteroSincronizacionDto>> Sincronizar()
    {
        var emisor = await RequireEmisorAsync();
        if (emisor is null) return Forbid();

        return await portero.SyncAllActiveSociosAsync(emisor.Id);
    }

    [HttpPost("abrir-puerta")]
    public async Task<ActionResult<PorteroAccionDto>> AbrirPuerta()
    {
        var emisor = await RequireEmisorAsync();
        if (emisor is null) return Forbid();

        var config = await portero.GetConfigAsync(emisor.Id);
        if (config is null || !config.Habilitado)
            return BadRequest(new { mensaje = "Portero no habilitado", errores = new[] { "Portero no habilitado" } });

        return await portero.AbrirPuertaAsync(config);
    }

    private async Task<Emisor?> RequireEmisorAsync()
    {
        if (!tenant.EmisorId.HasValue) return null;
        return await db.Emisores.FirstOrDefaultAsync(e => e.Id == tenant.EmisorId && e.Activo);
    }

    private string? GetApiPublicBaseUrl()
    {
        var env = Environment.GetEnvironmentVariable("API_PUBLIC_BASE_URL");
        if (!string.IsNullOrWhiteSpace(env)) return env.Trim().TrimEnd('/');
        return $"{Request.Scheme}://{Request.Host}";
    }
}
