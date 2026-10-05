using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsadaSJM.Models
{
    public class GaleriaDTO
    {
        [Key]
        public int IdGaleria { get; set; }
        public int IdUsuario { get; set; }

        [Display(Name = "Título")]
        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(200, ErrorMessage = "El título no puede superar los 200 caracteres.")]
        public string Titulo { get; set; } = string.Empty;

        [Display(Name = "Descripción")]
        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
        public string Descripcion { get; set; } = string.Empty;

        [Display(Name = "Fecha")]
        [DataType(DataType.Date)]
        public DateTime? Fecha { get; set; }
        public bool Estado { get; set; }

        public List<FotoGaleriaDTO> Fotos { get; set; } = new List<FotoGaleriaDTO>();
    }
}
