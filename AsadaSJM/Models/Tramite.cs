using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsadaSJM.Models;

public class Tramite
{
    [Key]
    public int IdTramite { get; set; }

    public int IdAbonado { get; set; }
    [ForeignKey(nameof(IdAbonado))]
    public Abonado? Abonado { get; set; }

    public int? IdUsuario { get; set; }
    [ForeignKey(nameof(IdUsuario))]
    public Usuario? Gestor { get; set; }

    [Required, StringLength(100)]
    public string TipoTramite { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string Estado { get; set; } = "Pendiente";

    public DateTime FechaSolicitud { get; set; } = DateTime.Now;

    public ICollection<Documento> Documentos { get; set; } = new List<Documento>();
}
