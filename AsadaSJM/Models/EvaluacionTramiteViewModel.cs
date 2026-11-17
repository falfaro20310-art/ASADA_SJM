namespace AsadaSJM.Models.ViewModels;

public class EvaluacionTramiteListaViewModel
{
    public int IdTramite { get; set; }
    public string NumeroTramite { get; set; } = string.Empty;
    public string NombreSolicitante { get; set; } = string.Empty;
    public DateTime FechaSolicitud { get; set; }
    public string Estado { get; set; } = string.Empty;
    public bool EsSegregacionFraccionamiento { get; set; }
}

public class DocumentoEvaluacionViewModel
{
    public int IdDocumento { get; set; }
    public string TipoDocumento { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public string RutaArchivo { get; set; } = string.Empty;
    public DateTime? FechaEmision { get; set; }
    public int? VigenciaDias { get; set; }

    public bool EstaVencido
    {
        get
        {
            if (!FechaEmision.HasValue || !VigenciaDias.HasValue)
            {
                return false;
            }

            return FechaEmision.Value.Date
                .AddDays(VigenciaDias.Value) < DateTime.Today;
        }
    }
}

public class EvaluacionTramiteViewModel
{
    public int IdTramite { get; set; }
    public string NumeroTramite { get; set; } = string.Empty;
    public string NombreSolicitante { get; set; } = string.Empty;
    public string PropositoDisponibilidad { get; set; } = string.Empty;
    public string DescripcionProyecto { get; set; } = string.Empty;
    public bool EsPropietarioRegistral { get; set; }
    public bool EsSegregacionFraccionamiento { get; set; }
    public DateTime FechaSolicitud { get; set; }
    public string Estado { get; set; } = string.Empty;

    public List<DocumentoEvaluacionViewModel> Documentos { get; set; } = new();

    public bool Requisito1 { get; set; }
    public bool Requisito2 { get; set; }
    public bool Requisito3 { get; set; }
    public bool Requisito4 { get; set; }
    public bool Requisito5 { get; set; }
    public bool Requisito6 { get; set; }
    public bool Requisito7 { get; set; }
    public bool Requisito8 { get; set; }
    public bool Requisito9 { get; set; }

    public string? MotivoRechazo { get; set; }

    public bool TodosRequisitosCumplidos =>
        Requisito1 &&
        Requisito2 &&
        Requisito3 &&
        Requisito4 &&
        Requisito5 &&
        Requisito6 &&
        Requisito7 &&
        Requisito8 &&
        Requisito9;
}
