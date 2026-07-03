using System.Text.Json;
using GestionSpa.Api.DTOs;
using GestionSpa.Api.Models;
using GestionSpa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionSpa.Api.Controllers;

[ApiController]
[Route("api/admin/backup")]
[Authorize(Roles = nameof(RolUsuario.SuperAdmin))]
public class AdminBackupController(IPlatformBackupService platformBackup) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        var package = await platformBackup.ExportAsync();
        var json = JsonSerializer.Serialize(package, JsonOpts);
        var fecha = package.ExportedAt.ToString("yyyyMMdd-HHmm");
        return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", $"gestionspa-platform-backup-{fecha}.json");
    }

    [HttpGet("resumen")]
    public async Task<ActionResult<PlatformBackupResumenDto>> Resumen()
    {
        return await platformBackup.GetResumenAsync();
    }

    [HttpPost("import")]
    [RequestSizeLimit(104_857_600)]
    public async Task<ActionResult<PlatformImportResultDto>> Import(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { mensaje = "Debés seleccionar un archivo JSON de respaldo", errores = new[] { "Archivo vacío" } });

        PlatformBackupPackage backup;
        try
        {
            await using var stream = file.OpenReadStream();
            backup = await JsonSerializer.DeserializeAsync<PlatformBackupPackage>(stream, JsonOpts)
                ?? throw new InvalidOperationException("Archivo JSON vacío o inválido");
        }
        catch (JsonException ex)
        {
            return BadRequest(new { mensaje = "El archivo JSON no es válido", errores = new[] { ex.Message } });
        }

        try
        {
            return await platformBackup.ImportAsync(backup);
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
