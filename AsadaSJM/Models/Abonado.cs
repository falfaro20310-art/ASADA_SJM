using System.ComponentModel.DataAnnotations;

namespace AsadaSJM.Models;

public class Abonado
{
    [Key]
    public int IdAbonado { get; set; }

    [Required, StringLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Direccion { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string NumeroMedidor { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public ICollection<Averia> Averias { get; set; } = new List<Averia>();
    public ICollection<Tramite> Tramites { get; set; } = new List<Tramite>();
    public ICollection<Recibo> Recibos { get; set; } = new List<Recibo>();
}
