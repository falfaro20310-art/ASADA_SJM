using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsadaSJM.Models;

public class Recibo
{
    [Key]
    public int IdRecibo { get; set; }

    public int IdAbonado { get; set; }
    [ForeignKey(nameof(IdAbonado))]
    public Abonado? Abonado { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Monto { get; set; }

    public DateTime FechaEmision { get; set; } = DateTime.Now;
}
