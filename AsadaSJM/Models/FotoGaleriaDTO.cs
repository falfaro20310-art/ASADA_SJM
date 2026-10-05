using System.ComponentModel.DataAnnotations;

namespace AsadaSJM.Models
{
    public class FotoGaleriaDTO
    {
        [Key]
        public int IdFoto { get; set; }
        public int IdGaleria { get; set; }
        [Display(Name = "Imagen")]
        [Required(ErrorMessage = "La imagen es obligatoria.")]
        [StringLength(500, ErrorMessage = "La ruta de la imagen no puede superar los 500 caracteres.")]
        public string RutaImagen { get; set; } = string.Empty;
        [Display(Name = "Descripción")]
        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(250, ErrorMessage = "La descripción no puede superar los 250 caracteres.")]
        public string Descripcion { get; set; } = string.Empty;
        public DateTime? Fecha { get; set; }
        public bool Estado { get; set; }
    }
}
