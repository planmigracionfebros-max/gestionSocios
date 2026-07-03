namespace GestionSpa.Api.DTOs;

public class EmisorBackupPackage
{
    public int Version { get; set; } = 1;
    public DateTime ExportedAt { get; set; }
    public EmisorBackupEmisor Emisor { get; set; } = null!;
    public EmisorBackupPortero? Portero { get; set; }
    public List<EmisorBackupUsuario> Usuarios { get; set; } = [];
    public List<EmisorBackupFamilia> Familias { get; set; } = [];
    public List<EmisorBackupSocio> Socios { get; set; } = [];
    public List<EmisorBackupCliente> Clientes { get; set; } = [];
    public List<EmisorBackupServicio> Servicios { get; set; } = [];
    public List<EmisorBackupCuota> Cuotas { get; set; } = [];
    public List<EmisorBackupCargo> Cargos { get; set; } = [];
    public List<EmisorBackupPago> Pagos { get; set; } = [];
    public List<EmisorBackupIngreso> Ingresos { get; set; } = [];
}

public class EmisorBackupEmisor
{
    public int SourceId { get; set; }
    public string Nombre { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Ciudad { get; set; }
    public string? Departamento { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaAlta { get; set; }
}

public class EmisorBackupPortero
{
    public bool Habilitado { get; set; }
    public string ApiUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string? WebhookSecret { get; set; }
    public string DeviceSn { get; set; } = "";
    public bool SincronizarAutomatico { get; set; }
    public DateTime FechaActualizacion { get; set; }
}

public class EmisorBackupUsuario
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Rol { get; set; } = "";
    public bool Activo { get; set; }
    public DateTime FechaAlta { get; set; }
}

public class EmisorBackupFamilia
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public decimal CuotaMensual { get; set; }
    public string? Observaciones { get; set; }
}

public class EmisorBackupSocio
{
    public int Id { get; set; }
    public string NumeroSocio { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Cedula { get; set; } = "";
    public string TipoIdentificacion { get; set; } = "";
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? Ciudad { get; set; }
    public string? Departamento { get; set; }
    public DateTime FechaAlta { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public decimal CuotaMensual { get; set; }
    public string MedioPago { get; set; } = "";
    public string Estado { get; set; } = "";
    public string? Observaciones { get; set; }
    public int? FamiliaId { get; set; }
}

public class EmisorBackupCliente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string? Cedula { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Ciudad { get; set; }
    public DateTime FechaRegistro { get; set; }
    public string? Observaciones { get; set; }
}

public class EmisorBackupServicio
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string Categoria { get; set; } = "";
    public decimal Precio { get; set; }
    public int DuracionMinutos { get; set; }
    public bool Activo { get; set; }
    public bool SoloSocios { get; set; }
}

public class EmisorBackupCuota
{
    public int Id { get; set; }
    public int SocioId { get; set; }
    public int Mes { get; set; }
    public int Anio { get; set; }
    public decimal MontoCuota { get; set; }
    public decimal MontoServicios { get; set; }
    public decimal MontoPagado { get; set; }
    public string EstadoPago { get; set; } = "";
    public DateTime? FechaVencimiento { get; set; }
    public DateTime? FechaPago { get; set; }
}

public class EmisorBackupCargo
{
    public int Id { get; set; }
    public int ServicioId { get; set; }
    public int? SocioId { get; set; }
    public int? ClienteId { get; set; }
    public int? CuotaMensualId { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public int Cantidad { get; set; }
    public string EstadoPago { get; set; } = "";
    public bool SumarACuota { get; set; }
    public string? Notas { get; set; }
    public string? AtendidoPor { get; set; }
}

public class EmisorBackupPago
{
    public int Id { get; set; }
    public int? CargoId { get; set; }
    public int? CuotaMensualId { get; set; }
    public decimal Monto { get; set; }
    public string MetodoPago { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string? Referencia { get; set; }
    public string? RegistradoPor { get; set; }
    public string? Notas { get; set; }
}

public class EmisorBackupIngreso
{
    public int Id { get; set; }
    public int SocioId { get; set; }
    public DateTime FechaHora { get; set; }
    public string Tipo { get; set; } = "";
    public bool AccesoPermitido { get; set; }
    public string? MotivoRechazo { get; set; }
}

public record EmisorBackupResumenDto(
    string EmisorNombre, string EmisorSlug, DateTime ExportedAt,
    int Usuarios, int Familias, int Socios, int Clientes, int Servicios,
    int Cuotas, int Cargos, int Pagos, int Ingresos);

public record EmisorImportResultDto(
    string Mensaje, int Usuarios, int Familias, int Socios, int Clientes,
    int Servicios, int Cuotas, int Cargos, int Pagos, int Ingresos);
