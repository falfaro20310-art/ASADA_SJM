using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsadaSJM.Models;

public class Usuario
{
    [Key]
    public int IdUsuario { get; set; }

    [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(150), EmailAddress]
    public string Correo { get; set; } = string.Empty;

    [Required, StringLength(256)]
    public string ContrasenaHash { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public int IdRol { get; set; }

    [ForeignKey(nameof(IdRol))]
    public Rol? Rol { get; set; }
}
