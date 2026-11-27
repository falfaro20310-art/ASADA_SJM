using AsadaSJM.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsadaSJM.Data.Entidades
{
    [Table("PAGO")]
    public class PagoEntidad
    {
        [Key]
        public int IdPago { get; set; }
        public int IdAbonado { get; set; }
        [ForeignKey(nameof(IdAbonado))]
        public Abonado? Abonado { get; set; }
        public int IdRecibo { get; set; }
        [ForeignKey(nameof(IdRecibo))]
        public Recibo? Recibo { get; set; }
        public int IdFormaPago { get; set; }
        [ForeignKey(nameof(IdFormaPago))]
        public FormaPagoEntidad? FormaPago { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaPago { get; set; }
        public string MetodoPago { get; set; } = string.Empty;
        public string Referencia { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int? IdUsuarioRegistro { get; set; }

        public List<ComprobantePagoEntidad> ComprobantePagos { get; set; } = new List<ComprobantePagoEntidad>();
    }
}
