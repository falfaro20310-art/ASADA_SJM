namespace AsadaSJM.Models;

public class UsuarioListadoViewModel
{
    public string IdNetUser { get; set; } = string.Empty;

    public string NombreCompleto { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    public string Rol { get; set; } = string.Empty;

    public bool Estado { get; set; }
}