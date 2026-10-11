namespace AsadaSJM.Models;

public class CategoriaDocumento
{
    public int IdCategoria { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public bool Estado { get; set; }
}
