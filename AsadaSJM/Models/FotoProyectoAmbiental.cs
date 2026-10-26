
using System.ComponentModel.DataAnnotations;

namespace AsadaSJM.Models;

public class FotoProyectoAmbiental
{
    public int IdFoto { get; set; }

    [Required(ErrorMessage = "El nombre del proyecto es obligatorio.")]
    [StringLength(150)]
    public string Titulo { get; set; } = string.Empty;

    public string? ImagenUrl { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}
