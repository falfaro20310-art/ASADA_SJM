using System.ComponentModel.DataAnnotations;

namespace AsadaSJM.Models;

public class Abonado
{
    [Key]
    public int IdAbonado { get; set; }

    [Required]
    public int IdUsuario { get; set; }

    [Required]
    [StringLength(30)]
    public string NIS { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string NumeroPaja { get; set; } = string.Empty;

    [StringLength(30)]
    public string? NumeroFinca { get; set; }

    [StringLength(30)]
    public string? NumeroPlano { get; set; }

    [Required]
    [StringLength(500)]
    public string Direccion { get; set; } = string.Empty;

    [Required]
    public int IdTarifa { get; set; }

    [Required]
    public int IdSector { get; set; }

    public ICollection<Averia> Averias { get; set; } = new List<Averia>();

    public ICollection<Tramite> Tramites { get; set; } = new List<Tramite>();

    public ICollection<Recibo> Recibos { get; set; } = new List<Recibo>();
}