using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace AsadaSJM.Models;

public class DocumentoTransparenciaViewModel
{
    [Required(
        ErrorMessage = "Debe ingresar el título del documento.")]
    [StringLength(
        200,
        ErrorMessage = "El título no puede superar los 200 caracteres.")]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = string.Empty;


    [Required(
        ErrorMessage = "Debe seleccionar una categoría.")]
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Debe seleccionar una categoría válida.")]
    [Display(Name = "Categoría")]
    public int IdCategoria { get; set; }


    [StringLength(
        500,
        ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }


    [StringLength(
        50,
        ErrorMessage = "El período no puede superar los 50 caracteres.")]
    [Display(Name = "Período")]
    public string? Periodo { get; set; }


    [Required(
        ErrorMessage = "Debe indicar la fecha de publicación.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de publicación")]
    public DateTime FechaPublicacion { get; set; } =
        DateTime.Today;


    [Required(
        ErrorMessage = "Debe seleccionar un archivo PDF.")]
    [Display(Name = "Archivo PDF")]
    public IFormFile? ArchivoPDF { get; set; }


    public List<CategoriaDocumento> Categorias { get; set; } =
        new();
}