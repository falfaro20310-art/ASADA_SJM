using System.ComponentModel.DataAnnotations;

namespace AsadaSJM.Models.Pagos
{
    public class PagoDTO
    {
        [Key]
        public int IdPago { get; set; }
        public int IdAbonado { get; set; }
        public int IdRecibo { get; set; }
        public int IdFormaPago { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaPago { get; set; }
        public string MetodoPago { get; set; } = string.Empty;
        public string Referencia { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int? IdUsuarioRegistro { get; set; }

        public List<ComprobantePagoEntidad> ComprobantePagos { get; set; } = new List<ComprobantePagoEntidad>();
    }
}
