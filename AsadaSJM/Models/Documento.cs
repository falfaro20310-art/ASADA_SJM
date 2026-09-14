using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsadaSJM.Models;

public class Documento
{
    [Key]
    public int IdDocumento { get; set; }

    public int? IdTramite { get; set; }
    [ForeignKey(nameof(IdTramite))]
    public Tramite? Tramite { get; set; }

    [Required, StringLength(200)]
    public string NombreArchivo { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Categoria { get; set; } = "General";

    public DateTime FechaCarga { get; set; } = DateTime.Now;
}
