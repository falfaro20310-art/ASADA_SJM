
using System.ComponentModel.DataAnnotations;

namespace AsadaSJM.Models;

public class CampanaAmbiental
{
    public int IdCampana { get; set; }

    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(150)]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha es obligatoria.")]
    [DataType(DataType.Date)]
    public DateTime Fecha { get; set; }

    [Required(ErrorMessage = "El lugar es obligatorio.")]
    [StringLength(200)]
    public string Lugar { get; set; } = string.Empty;

    // Ruta generada al subir la imagen.
    public string? ImagenUrl { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}
