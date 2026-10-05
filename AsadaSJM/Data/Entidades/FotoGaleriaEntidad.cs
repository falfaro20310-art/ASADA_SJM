using AsadaSJM.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsadaSJM.Data.Entidades
{
    [Table("FOTO_GALERIA")]
    public class FotoGaleriaEntidad
    {
        [Key]
        public int IdFoto { get; set; }
        public int IdGaleria { get; set; }
        [ForeignKey(nameof(IdGaleria))]
        public GaleriaEntidad? Galeria { get; set; }
        public string RutaImagen { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public DateTime? Fecha { get; set; }
        public bool Estado { get; set; }
    }
}
