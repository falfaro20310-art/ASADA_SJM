using AsadaSJM.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsadaSJM.Data.Entidades
{
    [Table("COMPROBANTE_PAGO")]
    public class ComprobantePagoEntidad
    {
        [Key]
        public int IdComprobante { get; set; }
        public int IdPago { get; set; }
        [ForeignKey(nameof(IdPago))]
        public PagoEntidad? Pago { get; set; }
        public string RutaArchivo { get; set; } = string.Empty;
        public DateTime FechaCarga { get; set; }
    }
}
