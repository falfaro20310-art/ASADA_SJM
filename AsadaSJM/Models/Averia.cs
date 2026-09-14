using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsadaSJM.Models;

public class Averia
{
    [Key]
    public int IdAveria { get; set; }

    public int IdAbonado { get; set; }
    [ForeignKey(nameof(IdAbonado))]
    public Abonado? Abonado { get; set; }

    public int? IdUsuario { get; set; }
    [ForeignKey(nameof(IdUsuario))]
    public Usuario? Responsable { get; set; }

    [Required, StringLength(300)]
    public string Descripcion { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string Estado { get; set; } = "Pendiente";

    public DateTime FechaReporte { get; set; } = DateTime.Now;
}
