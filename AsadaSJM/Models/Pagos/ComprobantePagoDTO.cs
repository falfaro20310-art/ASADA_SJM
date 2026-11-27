using System.ComponentModel.DataAnnotations;

namespace AsadaSJM.Models.Pagos
{
    public class ComprobantePagoEntidad
    {
        [Key]
        public int IdComprobante { get; set; }
        public int IdPago { get; set; }
        public string RutaArchivo { get; set; } = string.Empty;
        public DateTime FechaCarga { get; set; }
    }
}
