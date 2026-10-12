
using System;
using System.ComponentModel.DataAnnotations;

namespace AsadaSJM.Models;

public class Naciente
{
    [Key]
    public int IdNaciente { get; set; }

    [Required]
    [StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [StringLength(250)]
    public string Ubicacion { get; set; } = string.Empty;

    [Required]
    public string Descripcion { get; set; } = string.Empty;

    public bool Estado { get; set; } = true;

    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}
