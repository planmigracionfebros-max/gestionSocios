using GestionSpa.Api.Data;
using GestionSpa.Api.DTOs;
using GestionSpa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionSpa.Api.Services;

public interface IPlatformBackupService
{
    Task<PlatformBackupPackage> ExportAsync();
    Task<PlatformBackupResumenDto> GetResumenAsync();
    Task<PlatformImportResultDto> ImportAsync(PlatformBackupPackage backup);
}

public class PlatformBackupService(AppDbContext db, IEmisorBackupService emisorBackup) : IPlatformBackupService
{
    private const int CurrentVersion = 2;

    public async Task<PlatformBackupPackage> ExportAsync()
    {
        var emisorIds = await db.Emisores.OrderBy(e => e.Slug).Select(e => e.Id).ToListAsync();
        var emisores = new List<EmisorBackupPackage>();
        foreach (var id in emisorIds)
            emisores.Add(await emisorBackup.ExportAsync(id));

        var superAdmins = await db.Usuarios.AsNoTracking()
            .Where(u => u.Rol == RolUsuario.SuperAdmin)
            .Select(u => new EmisorBackupUsuario
            {
                Id = u.Id,
                Email = u.Email,
                PasswordHash = u.PasswordHash,
                Nombre = u.Nombre,
                Rol = u.Rol.ToString(),
                Activo = u.Activo,
                FechaAlta = u.FechaAlta,
            }).ToListAsync();

        return new PlatformBackupPackage
        {
            Version = CurrentVersion,
            ExportedAt = DateTime.UtcNow,
            SuperAdmins = superAdmins,
            Emisores = emisores,
        };
    }

    public async Task<PlatformBackupResumenDto> GetResumenAsync()
    {
        var emisores = await db.Emisores.AsNoTracking().OrderBy(e => e.Slug).ToListAsync();
        var superAdmins = await db.Usuarios.CountAsync(u => u.Rol == RolUsuario.SuperAdmin);
        var detalle = new List<PlatformEmisorResumenDto>();

        var totalSocios = 0;
        var totalCuotas = 0;
        var totalCargos = 0;
        var totalPagos = 0;
        var totalIngresos = 0;

        foreach (var emisor in emisores)
        {
            var socios = await db.Socios.CountAsync(s => s.EmisorId == emisor.Id);
            var usuarios = await db.Usuarios.CountAsync(u => u.EmisorId == emisor.Id);
            totalSocios += socios;
            totalCuotas += await db.CuotasMensuales.CountAsync(c => c.EmisorId == emisor.Id);
            totalCargos += await db.Cargos.CountAsync(c => c.EmisorId == emisor.Id);
            totalPagos += await db.Pagos.CountAsync(p => p.EmisorId == emisor.Id);
            totalIngresos += await db.Ingresos.CountAsync(i => i.EmisorId == emisor.Id);
            detalle.Add(new PlatformEmisorResumenDto(emisor.Slug, emisor.Nombre, socios, usuarios));
        }

        return new PlatformBackupResumenDto(
            DateTime.UtcNow,
            emisores.Count,
            superAdmins,
            totalSocios,
            totalCuotas,
            totalCargos,
            totalPagos,
            totalIngresos,
            detalle);
    }

    public async Task<PlatformImportResultDto> ImportAsync(PlatformBackupPackage backup)
    {
        if (backup.Version != CurrentVersion)
            throw new InvalidOperationException($"Versión de respaldo no soportada: {backup.Version}");

        if (backup.Emisores.Count == 0)
            throw new InvalidOperationException("El respaldo no contiene emisores");

        var slugs = backup.Emisores.Select(e => e.Emisor.Slug).ToList();
        if (slugs.Count != slugs.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            throw new InvalidOperationException("El respaldo contiene slugs de emisor duplicados");

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            await ClearPlatformAsync();

            foreach (var admin in backup.SuperAdmins)
            {
                db.Usuarios.Add(new Usuario
                {
                    Email = admin.Email,
                    PasswordHash = admin.PasswordHash,
                    Nombre = admin.Nombre,
                    Rol = RolUsuario.SuperAdmin,
                    Activo = admin.Activo,
                    FechaAlta = admin.FechaAlta,
                });
            }
            await db.SaveChangesAsync();

            foreach (var pkg in backup.Emisores)
            {
                if (pkg.Emisor is null)
                    throw new InvalidOperationException("Un emisor del respaldo no tiene datos");

                var emisor = new Emisor
                {
                    Nombre = pkg.Emisor.Nombre,
                    Slug = pkg.Emisor.Slug,
                    Ciudad = pkg.Emisor.Ciudad,
                    Departamento = pkg.Emisor.Departamento,
                    Activo = pkg.Emisor.Activo,
                    FechaAlta = pkg.Emisor.FechaAlta,
                };
                db.Emisores.Add(emisor);
                await db.SaveChangesAsync();

                if (pkg.Portero is not null)
                {
                    db.EmisorPorteroConfigs.Add(new EmisorPorteroConfig
                    {
                        EmisorId = emisor.Id,
                        Habilitado = pkg.Portero.Habilitado,
                        ApiUrl = pkg.Portero.ApiUrl,
                        ApiKey = pkg.Portero.ApiKey,
                        WebhookSecret = pkg.Portero.WebhookSecret,
                        DeviceSn = pkg.Portero.DeviceSn,
                        SincronizarAutomatico = pkg.Portero.SincronizarAutomatico,
                        FechaActualizacion = pkg.Portero.FechaActualizacion,
                    });
                    await db.SaveChangesAsync();
                }

                await emisorBackup.ImportEmisorContentAsync(emisor.Id, pkg);
            }

            await tx.CommitAsync();

            return new PlatformImportResultDto(
                "Plataforma importada correctamente",
                backup.Emisores.Count,
                backup.SuperAdmins.Count,
                backup.Emisores.Sum(e => e.Socios.Count),
                backup.Emisores.Sum(e => e.Cuotas.Count),
                backup.Emisores.Sum(e => e.Cargos.Count),
                backup.Emisores.Sum(e => e.Pagos.Count),
                backup.Emisores.Sum(e => e.Ingresos.Count));
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task ClearPlatformAsync()
    {
        await db.Pagos.ExecuteDeleteAsync();
        await db.Ingresos.ExecuteDeleteAsync();
        await db.Cargos.ExecuteDeleteAsync();
        await db.CuotasMensuales.ExecuteDeleteAsync();
        await db.Socios.ExecuteDeleteAsync();
        await db.Clientes.ExecuteDeleteAsync();
        await db.Servicios.ExecuteDeleteAsync();
        await db.Familias.ExecuteDeleteAsync();
        await db.EmisorPorteroConfigs.ExecuteDeleteAsync();
        await db.Usuarios.ExecuteDeleteAsync();
        await db.Emisores.ExecuteDeleteAsync();
    }

    public static PlatformBackupResumenDto ToResumen(PlatformBackupPackage package) => new(
        package.ExportedAt,
        package.Emisores.Count,
        package.SuperAdmins.Count,
        package.Emisores.Sum(e => e.Socios.Count),
        package.Emisores.Sum(e => e.Cuotas.Count),
        package.Emisores.Sum(e => e.Cargos.Count),
        package.Emisores.Sum(e => e.Pagos.Count),
        package.Emisores.Sum(e => e.Ingresos.Count),
        package.Emisores.Select(e => new PlatformEmisorResumenDto(
            e.Emisor.Slug, e.Emisor.Nombre, e.Socios.Count, e.Usuarios.Count)).ToList());
}
