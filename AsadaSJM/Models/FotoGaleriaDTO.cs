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
    }
}
