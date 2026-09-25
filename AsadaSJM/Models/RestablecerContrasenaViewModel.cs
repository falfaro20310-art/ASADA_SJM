using System.ComponentModel.DataAnnotations;

namespace AsadaSJM.Models;

public class RestablecerContrasenaViewModel
{
    public string Token { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string NuevaContrasena { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string ConfirmarContrasena { get; set; } = string.Empty;
}
