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


    // =========================================================
    // MTG-02
    // =========================================================

    [Display(Name = "¿La solicitud corresponde a segregación o fraccionamiento?")]
    public bool EsSegregacionFraccionamiento { get; set; }


    [Required(ErrorMessage = "Debe indicar la fecha de emisión de la certificación literal.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de emisión de la certificación literal")]
    public DateTime? FechaEmisionCertificacionLiteral { get; set; }


    [Required(ErrorMessage = "Debe adjuntar la certificación literal vigente.")]
    [Display(Name = "Certificación literal")]
    public IFormFile? CertificacionLiteral { get; set; }


    [Display(Name = "Plano de agrimensura de las fracciones")]
    public IFormFile? PlanoAgrimensuraFracciones { get; set; }


    [Display(Name = "Minuta de rechazo")]
    public IFormFile? MinutaRechazo { get; set; }
}