
using System.ComponentModel.DataAnnotations;

namespace AsadaSJM.Models;

public class InscripcionVoluntariado
{
    public int IdInscripcion { get; set; }

    [Required(ErrorMessage = "Ingresa tu nombre.")]
    [StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
    [StringLength(200)]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu teléfono.")]
    [RegularExpression(
        @"^[0-9+()\s-]{8,25}$",
        ErrorMessage = "Ingresa un teléfono válido.")]
    [StringLength(25)]
    public string Telefono { get; set; } = string.Empty;

    // Opcional: permite identificar la campaña de interés.
    public int? IdCampana { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}
