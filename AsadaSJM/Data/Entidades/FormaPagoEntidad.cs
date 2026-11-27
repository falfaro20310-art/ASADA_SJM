using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsadaSJM.Data.Entidades
{
    [Table("FORMA_PAGO")]
    public class FormaPagoEntidad
    {
        [Key]
        public int IdFormaPago { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Estado { get; set; }

        public List<PagoEntidad> Pagos { get; set; } = new List<PagoEntidad>();
    }
}
