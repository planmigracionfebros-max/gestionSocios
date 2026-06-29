namespace GestionSpa.Api.Models;

public class Familia
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal CuotaMensual { get; set; }
    public string? Observaciones { get; set; }

    public ICollection<Socio> Socios { get; set; } = [];
}
