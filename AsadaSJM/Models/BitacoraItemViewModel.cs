using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsadaSJM.Models;

public class BitacoraItemViewModel
{
    [Key]
    public int IdBitacora { get; set; }
     
    public int IdUsuario { get; set; }
    [ForeignKey(nameof(IdUsuario))]
    public Usuario? Usuario { get; set; }

    [Required, StringLength(200)]
    public string Accion { get; set; } = string.Empty;

    [Column("Fecha")]
    public DateTime FechaHora { get; set; } = DateTime.Now;
}
