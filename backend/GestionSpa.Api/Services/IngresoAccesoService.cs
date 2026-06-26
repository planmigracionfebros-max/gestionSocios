using GestionSpa.Api.Data;
using GestionSpa.Api.Models;

namespace GestionSpa.Api.Services;

public class IngresoAccesoService(CuotaService cuotaService)
{
    public record ResultadoAcceso(bool Permitido, string? MotivoRechazo, CuotaMensual? Cuota);

    public async Task<ResultadoAcceso> EvaluarAccesoSocioAsync(Socio socio)
    {
        if (socio.Estado != EstadoSocio.Activo)
            return new ResultadoAcceso(false, $"Socio {socio.Estado.ToString().ToLower()}", null);

        if (socio.FechaVencimiento.HasValue && socio.FechaVencimiento.Value.Date < UruguayTime.Today)
            return new ResultadoAcceso(false, "Membresía vencida", null);

        var (mes, anio) = UruguayTime.MesAnioActual();
        var cuota = await cuotaService.ObtenerOCrearCuotaAsync(socio.Id, mes, anio);

        if (UruguayTime.EsDespuesDelDia10())
        {
            if (cuota.EstadoPago != EstadoPago.Pagado)
                return new ResultadoAcceso(false, "Cuota del mes pendiente de pago", cuota);

            if (cuota.MontoPagado < cuota.Total)
                return new ResultadoAcceso(false, "Saldo de cuota pendiente", cuota);
        }

        return new ResultadoAcceso(true, null, cuota);
    }
}
