using System.ComponentModel.DataAnnotations;

namespace AsadaSJM.Models.ViewModels;

public sealed class ConsultaTramitesViewModel
{
    [Display(Name = "Número de trámite o número de paja")]
    public string Busqueda { get; set; } = string.Empty;

    public bool BusquedaRealizada { get; set; }

    public List<ConsultaTramiteResultadoViewModel> Resultados { get; set; } = new();
}

public sealed class ConsultaTramiteResultadoViewModel
{
    public int IdTramite { get; set; }
    public string NumeroTramite { get; set; } = string.Empty;
    public string NumeroPaja { get; set; } = string.Empty;
    public string TipoTramite { get; set; } = string.Empty;
    public DateTime FechaSolicitud { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime? FechaFinalizacion { get; set; }
    public string MotivoRechazo { get; set; } = string.Empty;
}
