using System.ComponentModel.DataAnnotations;

namespace AsadaSJM.Models.Pagos
{
    public class FormaPagoDTO
    {
        [Key]
        public int IdFormaPago { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }
}
