using System.ComponentModel.DataAnnotations;

namespace AsadaSJM.Models;

public class InformacionInstitucional
{
    [Key]
    public int IdInformacion { get; set; }

    public string? Historia { get; set; }

    public string? Mision { get; set; }

    public string? Vision { get; set; }

    [StringLength(200)]
    public string? Correo { get; set; }

    [StringLength(20)]
    public string? Telefono { get; set; }

    [StringLength(300)]
    public string? Facebook { get; set; }

    [StringLength(300)]
    public string? Instagram { get; set; }

    [StringLength(20)]
    public string? WhatsApp { get; set; }

    [StringLength(200)]
    public string? TikTok { get; set; }

    public DateTime FechaRegistro { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public int? IdUsuarioModificacion { get; set; }

    [StringLength(500)]
    public string? Direccion { get; set; }

    [StringLength(200)]
    public string? Horario { get; set; }
}