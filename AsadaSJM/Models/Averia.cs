using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsadaSJM.Models;

public class Averia
{
    [Key]
    public int IdAveria { get; set; }

    public int? IdAbonado { get; set; }

    [ForeignKey(nameof(IdAbonado))]
    public Abonado? Abonado { get; set; }

    [Required]
    public int IdUsuarioReporta { get; set; }

    [ForeignKey(nameof(IdUsuarioReporta))]
    public Usuario? UsuarioReporta { get; set; }

    [Required]
    public int IdCategoriaAveria { get; set; }

    [ForeignKey(nameof(IdCategoriaAveria))]
    public CategoriaAveria? CategoriaAveria { get; set; }

    [Required]
    public string Descripcion { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Ubicacion { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Prioridad { get; set; } = string.Empty;

    [Required]
    public DateTime FechaReporte { get; set; }
}

public class CategoriaAveria
{
    [Key]
    public int IdCategoriaAveria { get; set; }

    [Required]
    public string Nombre { get; set; } = string.Empty;
}
