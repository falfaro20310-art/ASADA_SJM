
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsadaSJM.Models;

public class Expresidente
{
    [Key]
    public int IdExpresidente { get; set; }

    [NotMapped]
    public int IdUsuario { get; set; }

    [Required]
    [StringLength(200)]
    public string Nombre { get; set; } = string.Empty;

    public string? Trayectoria { get; set; }

    public string? Proyectos { get; set; }

    [StringLength(500)]
    public string? Imagen { get; set; }

    [Required]
    public bool Estado { get; set; }
}
