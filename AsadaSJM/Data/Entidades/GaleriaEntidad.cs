using AsadaSJM.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsadaSJM.Data.Entidades
{
    [Table("GALERIA")]
    public class GaleriaEntidad
    {
        [Key]
        public int IdGaleria { get; set; }
        public int IdUsuario { get; set; }
        [ForeignKey(nameof(IdUsuario))]
        public Usuario? Usuario { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public DateTime? Fecha { get; set; }
        public bool Estado { get; set; }

        public ICollection<FotoGaleriaEntidad> Fotos { get; set; } = new List<FotoGaleriaEntidad>();
    }
}
