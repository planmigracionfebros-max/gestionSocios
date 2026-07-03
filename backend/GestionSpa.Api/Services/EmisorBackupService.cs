using System.Text.Json;
using GestionSpa.Api.Data;
using GestionSpa.Api.DTOs;
using GestionSpa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionSpa.Api.Services;

public interface IEmisorBackupService
{
    Task<EmisorBackupPackage> ExportAsync(int emisorId);
    Task<EmisorImportResultDto> ImportAsync(int emisorId, EmisorBackupPackage backup);
}

public class EmisorBackupService(AppDbContext db) : IEmisorBackupService
{
    private const int CurrentVersion = 1;

    public async Task<EmisorBackupPackage> ExportAsync(int emisorId)
    {
        var emisor = await db.Emisores.AsNoTracking().FirstOrDefaultAsync(e => e.Id == emisorId)
            ?? throw new InvalidOperationException("Emisor no encontrado");

        var portero = await db.EmisorPorteroConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.EmisorId == emisorId);

        return new EmisorBackupPackage
        {
            Version = CurrentVersion,
            ExportedAt = DateTime.UtcNow,
            Emisor = new EmisorBackupEmisor
            {
                SourceId = emisor.Id,
                Nombre = emisor.Nombre,
                Slug = emisor.Slug,
                Ciudad = emisor.Ciudad,
                Departamento = emisor.Departamento,
                Activo = emisor.Activo,
                FechaAlta = emisor.FechaAlta,
            },
            Portero = portero is null ? null : new EmisorBackupPortero
            {
                Habilitado = portero.Habilitado,
                ApiUrl = portero.ApiUrl,
                ApiKey = portero.ApiKey,
                WebhookSecret = portero.WebhookSecret,
                DeviceSn = portero.DeviceSn,
                SincronizarAutomatico = portero.SincronizarAutomatico,
                FechaActualizacion = portero.FechaActualizacion,
            },
            Usuarios = await db.Usuarios.AsNoTracking()
                .Where(u => u.EmisorId == emisorId)
                .Select(u => new EmisorBackupUsuario
                {
                    Id = u.Id,
                    Email = u.Email,
                    PasswordHash = u.PasswordHash,
                    Nombre = u.Nombre,
                    Rol = u.Rol.ToString(),
                    Activo = u.Activo,
                    FechaAlta = u.FechaAlta,
                }).ToListAsync(),
            Familias = await db.Familias.AsNoTracking().Where(f => f.EmisorId == emisorId)
                .Select(f => new EmisorBackupFamilia
                {
                    Id = f.Id,
                    Nombre = f.Nombre,
                    CuotaMensual = f.CuotaMensual,
                    Observaciones = f.Observaciones,
                }).ToListAsync(),
            Socios = await db.Socios.AsNoTracking().Where(s => s.EmisorId == emisorId)
                .Select(s => new EmisorBackupSocio
                {
                    Id = s.Id,
                    NumeroSocio = s.NumeroSocio,
                    Nombre = s.Nombre,
                    Apellido = s.Apellido,
                    Cedula = s.Cedula,
                    TipoIdentificacion = s.TipoIdentificacion.ToString(),
                    Telefono = s.Telefono,
                    Email = s.Email,
                    Direccion = s.Direccion,
                    Ciudad = s.Ciudad,
                    Departamento = s.Departamento,
                    FechaAlta = s.FechaAlta,
                    FechaVencimiento = s.FechaVencimiento,
                    CuotaMensual = s.CuotaMensual,
                    MedioPago = s.MedioPago.ToString(),
                    Estado = s.Estado.ToString(),
                    Observaciones = s.Observaciones,
                    FamiliaId = s.FamiliaId,
                }).ToListAsync(),
            Clientes = await db.Clientes.AsNoTracking().Where(c => c.EmisorId == emisorId)
                .Select(c => new EmisorBackupCliente
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Apellido = c.Apellido,
                    Cedula = c.Cedula,
                    Telefono = c.Telefono,
                    Email = c.Email,
                    Ciudad = c.Ciudad,
                    FechaRegistro = c.FechaRegistro,
                    Observaciones = c.Observaciones,
                }).ToListAsync(),
            Servicios = await db.Servicios.AsNoTracking().Where(s => s.EmisorId == emisorId)
                .Select(s => new EmisorBackupServicio
                {
                    Id = s.Id,
                    Nombre = s.Nombre,
                    Descripcion = s.Descripcion,
                    Categoria = s.Categoria.ToString(),
                    Precio = s.Precio,
                    DuracionMinutos = s.DuracionMinutos,
                    Activo = s.Activo,
                    SoloSocios = s.SoloSocios,
                }).ToListAsync(),
            Cuotas = await db.CuotasMensuales.AsNoTracking().Where(c => c.EmisorId == emisorId)
                .Select(c => new EmisorBackupCuota
                {
                    Id = c.Id,
                    SocioId = c.SocioId,
                    Mes = c.Mes,
                    Anio = c.Anio,
                    MontoCuota = c.MontoCuota,
                    MontoServicios = c.MontoServicios,
                    MontoPagado = c.MontoPagado,
                    EstadoPago = c.EstadoPago.ToString(),
                    FechaVencimiento = c.FechaVencimiento,
                    FechaPago = c.FechaPago,
                }).ToListAsync(),
            Cargos = await db.Cargos.AsNoTracking().Where(c => c.EmisorId == emisorId)
                .Select(c => new EmisorBackupCargo
                {
                    Id = c.Id,
                    ServicioId = c.ServicioId,
                    SocioId = c.SocioId,
                    ClienteId = c.ClienteId,
                    CuotaMensualId = c.CuotaMensualId,
                    Fecha = c.Fecha,
                    Monto = c.Monto,
                    Cantidad = c.Cantidad,
                    EstadoPago = c.EstadoPago.ToString(),
                    SumarACuota = c.SumarACuota,
                    Notas = c.Notas,
                    AtendidoPor = c.AtendidoPor,
                }).ToListAsync(),
            Pagos = await db.Pagos.AsNoTracking().Where(p => p.EmisorId == emisorId)
                .Select(p => new EmisorBackupPago
                {
                    Id = p.Id,
                    CargoId = p.CargoId,
                    CuotaMensualId = p.CuotaMensualId,
                    Monto = p.Monto,
                    MetodoPago = p.MetodoPago.ToString(),
                    Fecha = p.Fecha,
                    Referencia = p.Referencia,
                    RegistradoPor = p.RegistradoPor,
                    Notas = p.Notas,
                }).ToListAsync(),
            Ingresos = await db.Ingresos.AsNoTracking().Where(i => i.EmisorId == emisorId)
                .Select(i => new EmisorBackupIngreso
                {
                    Id = i.Id,
                    SocioId = i.SocioId,
                    FechaHora = i.FechaHora,
                    Tipo = i.Tipo.ToString(),
                    AccesoPermitido = i.AccesoPermitido,
                    MotivoRechazo = i.MotivoRechazo,
                }).ToListAsync(),
        };
    }

    public async Task<EmisorImportResultDto> ImportAsync(int emisorId, EmisorBackupPackage backup)
    {
        if (backup.Version != CurrentVersion)
            throw new InvalidOperationException($"Versión de respaldo no soportada: {backup.Version}");

        if (backup.Emisor is null)
            throw new InvalidOperationException("El respaldo no contiene datos del emisor");

        var emisor = await db.Emisores.FirstOrDefaultAsync(e => e.Id == emisorId)
            ?? throw new InvalidOperationException("Emisor destino no encontrado");

        if (await db.Emisores.AnyAsync(e => e.Slug == backup.Emisor.Slug && e.Id != emisorId))
            throw new InvalidOperationException($"Ya existe otro emisor con el slug '{backup.Emisor.Slug}'");

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            await ClearEmisorDataAsync(emisorId);

            emisor.Nombre = backup.Emisor.Nombre;
            emisor.Slug = backup.Emisor.Slug;
            emisor.Ciudad = backup.Emisor.Ciudad;
            emisor.Departamento = backup.Emisor.Departamento;
            emisor.Activo = backup.Emisor.Activo;
            emisor.FechaAlta = backup.Emisor.FechaAlta;

            if (backup.Portero is not null)
            {
                db.EmisorPorteroConfigs.Add(new EmisorPorteroConfig
                {
                    EmisorId = emisorId,
                    Habilitado = backup.Portero.Habilitado,
                    ApiUrl = backup.Portero.ApiUrl,
                    ApiKey = backup.Portero.ApiKey,
                    WebhookSecret = backup.Portero.WebhookSecret,
                    DeviceSn = backup.Portero.DeviceSn,
                    SincronizarAutomatico = backup.Portero.SincronizarAutomatico,
                    FechaActualizacion = backup.Portero.FechaActualizacion,
                });
            }

            await db.SaveChangesAsync();

            foreach (var u in backup.Usuarios.Where(u => u.Rol != nameof(RolUsuario.SuperAdmin)))
            {
                db.Usuarios.Add(new Usuario
                {
                    EmisorId = emisorId,
                    Email = u.Email,
                    PasswordHash = u.PasswordHash,
                    Nombre = u.Nombre,
                    Rol = Enum.Parse<RolUsuario>(u.Rol),
                    Activo = u.Activo,
                    FechaAlta = u.FechaAlta,
                });
            }
            await db.SaveChangesAsync();

            var familiaMap = new Dictionary<int, int>();
            foreach (var f in backup.Familias)
            {
                var entity = new Familia
                {
                    EmisorId = emisorId,
                    Nombre = f.Nombre,
                    CuotaMensual = f.CuotaMensual,
                    Observaciones = f.Observaciones,
                };
                db.Familias.Add(entity);
                await db.SaveChangesAsync();
                familiaMap[f.Id] = entity.Id;
            }

            var socioMap = new Dictionary<int, int>();
            foreach (var s in backup.Socios)
            {
                var entity = new Socio
                {
                    EmisorId = emisorId,
                    NumeroSocio = s.NumeroSocio,
                    Nombre = s.Nombre,
                    Apellido = s.Apellido,
                    Cedula = s.Cedula,
                    TipoIdentificacion = Enum.Parse<TipoIdentificacionSocio>(s.TipoIdentificacion),
                    Telefono = s.Telefono,
                    Email = s.Email,
                    Direccion = s.Direccion,
                    Ciudad = s.Ciudad,
                    Departamento = s.Departamento,
                    FechaAlta = s.FechaAlta,
                    FechaVencimiento = s.FechaVencimiento,
                    CuotaMensual = s.CuotaMensual,
                    MedioPago = Enum.Parse<MetodoPago>(s.MedioPago),
                    Estado = Enum.Parse<EstadoSocio>(s.Estado),
                    Observaciones = s.Observaciones,
                    FamiliaId = s.FamiliaId.HasValue && familiaMap.TryGetValue(s.FamiliaId.Value, out var fid) ? fid : null,
                };
                db.Socios.Add(entity);
                await db.SaveChangesAsync();
                socioMap[s.Id] = entity.Id;
            }

            var clienteMap = new Dictionary<int, int>();
            foreach (var c in backup.Clientes)
            {
                var entity = new Cliente
                {
                    EmisorId = emisorId,
                    Nombre = c.Nombre,
                    Apellido = c.Apellido,
                    Cedula = c.Cedula,
                    Telefono = c.Telefono,
                    Email = c.Email,
                    Ciudad = c.Ciudad,
                    FechaRegistro = c.FechaRegistro,
                    Observaciones = c.Observaciones,
                };
                db.Clientes.Add(entity);
                await db.SaveChangesAsync();
                clienteMap[c.Id] = entity.Id;
            }

            var servicioMap = new Dictionary<int, int>();
            foreach (var s in backup.Servicios)
            {
                var entity = new Servicio
                {
                    EmisorId = emisorId,
                    Nombre = s.Nombre,
                    Descripcion = s.Descripcion,
                    Categoria = Enum.Parse<CategoriaServicio>(s.Categoria),
                    Precio = s.Precio,
                    DuracionMinutos = s.DuracionMinutos,
                    Activo = s.Activo,
                    SoloSocios = s.SoloSocios,
                };
                db.Servicios.Add(entity);
                await db.SaveChangesAsync();
                servicioMap[s.Id] = entity.Id;
            }

            var cuotaMap = new Dictionary<int, int>();
            foreach (var c in backup.Cuotas)
            {
                if (!socioMap.TryGetValue(c.SocioId, out var socioId)) continue;
                var entity = new CuotaMensual
                {
                    EmisorId = emisorId,
                    SocioId = socioId,
                    Mes = c.Mes,
                    Anio = c.Anio,
                    MontoCuota = c.MontoCuota,
                    MontoServicios = c.MontoServicios,
                    MontoPagado = c.MontoPagado,
                    EstadoPago = Enum.Parse<EstadoPago>(c.EstadoPago),
                    FechaVencimiento = c.FechaVencimiento,
                    FechaPago = c.FechaPago,
                };
                db.CuotasMensuales.Add(entity);
                await db.SaveChangesAsync();
                cuotaMap[c.Id] = entity.Id;
            }

            var cargoMap = new Dictionary<int, int>();
            foreach (var c in backup.Cargos)
            {
                if (!servicioMap.TryGetValue(c.ServicioId, out var servicioId)) continue;
                var entity = new Cargo
                {
                    EmisorId = emisorId,
                    ServicioId = servicioId,
                    SocioId = c.SocioId.HasValue && socioMap.TryGetValue(c.SocioId.Value, out var sid) ? sid : null,
                    ClienteId = c.ClienteId.HasValue && clienteMap.TryGetValue(c.ClienteId.Value, out var cid) ? cid : null,
                    CuotaMensualId = c.CuotaMensualId.HasValue && cuotaMap.TryGetValue(c.CuotaMensualId.Value, out var qid) ? qid : null,
                    Fecha = c.Fecha,
                    Monto = c.Monto,
                    Cantidad = c.Cantidad,
                    EstadoPago = Enum.Parse<EstadoPago>(c.EstadoPago),
                    SumarACuota = c.SumarACuota,
                    Notas = c.Notas,
                    AtendidoPor = c.AtendidoPor,
                };
                db.Cargos.Add(entity);
                await db.SaveChangesAsync();
                cargoMap[c.Id] = entity.Id;
            }

            foreach (var p in backup.Pagos)
            {
                db.Pagos.Add(new Pago
                {
                    EmisorId = emisorId,
                    CargoId = p.CargoId.HasValue && cargoMap.TryGetValue(p.CargoId.Value, out var cargoid) ? cargoid : null,
                    CuotaMensualId = p.CuotaMensualId.HasValue && cuotaMap.TryGetValue(p.CuotaMensualId.Value, out var cuotaid) ? cuotaid : null,
                    Monto = p.Monto,
                    MetodoPago = Enum.Parse<MetodoPago>(p.MetodoPago),
                    Fecha = p.Fecha,
                    Referencia = p.Referencia,
                    RegistradoPor = p.RegistradoPor,
                    Notas = p.Notas,
                });
            }
            await db.SaveChangesAsync();

            foreach (var i in backup.Ingresos)
            {
                if (!socioMap.TryGetValue(i.SocioId, out var socioId)) continue;
                db.Ingresos.Add(new Ingreso
                {
                    EmisorId = emisorId,
                    SocioId = socioId,
                    FechaHora = i.FechaHora,
                    Tipo = Enum.Parse<TipoIngreso>(i.Tipo),
                    AccesoPermitido = i.AccesoPermitido,
                    MotivoRechazo = i.MotivoRechazo,
                });
            }
            await db.SaveChangesAsync();

            await tx.CommitAsync();

            return new EmisorImportResultDto(
                "Datos importados correctamente",
                backup.Usuarios.Count,
                backup.Familias.Count,
                backup.Socios.Count,
                backup.Clientes.Count,
                backup.Servicios.Count,
                backup.Cuotas.Count,
                backup.Cargos.Count,
                backup.Pagos.Count,
                backup.Ingresos.Count);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task ClearEmisorDataAsync(int emisorId)
    {
        await db.Pagos.Where(p => p.EmisorId == emisorId).ExecuteDeleteAsync();
        await db.Ingresos.Where(i => i.EmisorId == emisorId).ExecuteDeleteAsync();
        await db.Cargos.Where(c => c.EmisorId == emisorId).ExecuteDeleteAsync();
        await db.CuotasMensuales.Where(c => c.EmisorId == emisorId).ExecuteDeleteAsync();
        await db.Socios.Where(s => s.EmisorId == emisorId).ExecuteDeleteAsync();
        await db.Clientes.Where(c => c.EmisorId == emisorId).ExecuteDeleteAsync();
        await db.Servicios.Where(s => s.EmisorId == emisorId).ExecuteDeleteAsync();
        await db.Familias.Where(f => f.EmisorId == emisorId).ExecuteDeleteAsync();
        await db.Usuarios.Where(u => u.EmisorId == emisorId).ExecuteDeleteAsync();
        await db.EmisorPorteroConfigs.Where(c => c.EmisorId == emisorId).ExecuteDeleteAsync();
    }

    public static EmisorBackupResumenDto ToResumen(EmisorBackupPackage package) => new(
        package.Emisor.Nombre,
        package.Emisor.Slug,
        package.ExportedAt,
        package.Usuarios.Count,
        package.Familias.Count,
        package.Socios.Count,
        package.Clientes.Count,
        package.Servicios.Count,
        package.Cuotas.Count,
        package.Cargos.Count,
        package.Pagos.Count,
        package.Ingresos.Count);
}
