using System.ComponentModel.DataAnnotations;

namespace AsadaSJM.Models;

public class SolicitudDisponibilidadViewModel
{
    [Required(ErrorMessage = "El nombre del solicitante es obligatorio.")]
    [StringLength(200)]
    [Display(Name = "Nombre del solicitante")]
    public string NombreSolicitante { get; set; } = string.Empty;


    [Required(ErrorMessage = "El propósito de la disponibilidad es obligatorio.")]
    [StringLength(250)]
    [Display(Name = "Propósito de la disponibilidad")]
    public string PropositoDisponibilidad { get; set; } = string.Empty;


    [Required(ErrorMessage = "La descripción del proyecto es obligatoria.")]
    [StringLength(1000)]
    [Display(Name = "Descripción del proyecto")]
    public string DescripcionProyecto { get; set; } = string.Empty;


    [Display(Name = "¿Es propietario registral del inmueble?")]
    public bool EsPropietarioRegistral { get; set; }


    [Required(ErrorMessage = "Debe adjuntar una identificación vigente.")]
    [Display(Name = "Identificación vigente")]
    public IFormFile? Identificacion { get; set; }


    [Display(Name = "Poder especial o autorización autenticada")]
    public IFormFile? AutorizacionPropietario { get; set; }
}