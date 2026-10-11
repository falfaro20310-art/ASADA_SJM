namespace AsadaSJM.Models;

public class DocumentoTransparencia
{
    public int IdDocumento { get; set; }

    public int IdCategoria { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string? Periodo { get; set; }

    public string RutaArchivo { get; set; } = string.Empty;

    public DateTime FechaPublicacion { get; set; }

    public int IdUsuarioPublicador { get; set; }

    public bool Estado { get; set; }

    public string NombreCategoria { get; set; } = string.Empty;
}