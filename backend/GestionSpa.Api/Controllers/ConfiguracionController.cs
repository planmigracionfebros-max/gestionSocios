using System.Text.Json;
using GestionSpa.Api.DTOs;
using GestionSpa.Api.Models;
using GestionSpa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionSpa.Api.Controllers;

[ApiController]
[Route("api/configuracion")]
[Authorize(Roles = $"{nameof(RolUsuario.SuperAdmin)},{nameof(RolUsuario.AdminEmisor)}")]
public class ConfiguracionController(ITenantContext tenant, IEmisorBackupService backupService) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        if (tenant.Rol == RolUsuario.AdminEmisor && !tenant.EmisorId.HasValue)
            return Forbid();

        var emisorId = tenant.RequireEmisorId();
        var package = await backupService.ExportAsync(emisorId);
        var json = JsonSerializer.Serialize(package, JsonOpts);
        var slug = package.Emisor.Slug;
        var fecha = package.ExportedAt.ToString("yyyyMMdd-HHmm");
        var fileName = $"gestionspa-backup-{slug}-{fecha}.json";
        return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", fileName);
    }

    [HttpGet("export/resumen")]
    public async Task<ActionResult<EmisorBackupResumenDto>> ExportResumen()
    {
        if (tenant.Rol == RolUsuario.AdminEmisor && !tenant.EmisorId.HasValue)
            return Forbid();

        var emisorId = tenant.RequireEmisorId();
        var package = await backupService.ExportAsync(emisorId);
        return EmisorBackupService.ToResumen(package);
    }

    [HttpPost("import")]
    [RequestSizeLimit(52_428_800)]
    public async Task<ActionResult<EmisorImportResultDto>> Import(IFormFile file)
    {
        if (tenant.Rol == RolUsuario.AdminEmisor && !tenant.EmisorId.HasValue)
            return Forbid();

        if (file is null || file.Length == 0)
            return BadRequest(new { mensaje = "Debés seleccionar un archivo JSON de respaldo", errores = new[] { "Archivo vacío" } });

        if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(file.ContentType, "application/json", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { mensaje = "El archivo debe ser JSON", errores = new[] { "Formato inválido" } });

        EmisorBackupPackage backup;
        try
        {
            await using var stream = file.OpenReadStream();
            backup = await JsonSerializer.DeserializeAsync<EmisorBackupPackage>(stream, JsonOpts)
                ?? throw new InvalidOperationException("Archivo JSON vacío o inválido");
        }
        catch (JsonException ex)
        {
            return BadRequest(new { mensaje = "El archivo JSON no es válido", errores = new[] { ex.Message } });
        }

        try
        {
            var emisorId = tenant.RequireEmisorId();
            return await backupService.ImportAsync(emisorId, backup);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message, errores = new[] { ex.Message } });
        }
        catch (DbUpdateException ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return BadRequest(new { mensaje = "Error al guardar en la base de datos", errores = new[] { msg } });
        }
    }
}
