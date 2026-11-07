
using AsadaSJM.Models;

namespace AsadaSJM.Models.ViewModels;

public class PanelAdministrativoDashboardViewModel
{
    public int Ano { get; set; }

    public int? Mes { get; set; }

    public DateTime InicioPeriodo { get; set; }

    public DateTime FinPeriodoExclusivo { get; set; }

    public int TotalAbonadosActuales { get; set; }

    public int AbonadosRegistradosPeriodo { get; set; }

    public int AbonadosSinFechaRegistro { get; set; }

    public decimal ConsumoTotalM3 { get; set; }

    public decimal? LitrosPorPersonaDia { get; set; }

    public int AbonadosConConsumoSinCantidadPersonas { get; set; }

    public decimal? IndiceMoraPorcentaje { get; set; }

    public int RecibosEmitidosPeriodo { get; set; }

    public int RecibosVencidosPendientes { get; set; }

    public int AveriasRegistradas { get; set; }

    public int TramitesPendientes { get; set; }

    public List<ConsumoResumenDashboard> ConsumoPorSector
    { get; set; } = new();

    public List<ConsumoResumenDashboard> ConsumoPorTarifa
    { get; set; } = new();

    public List<ConsumoAbonadoDashboard> ConsumoPorAbonado
    { get; set; } = new();

    public List<Averia> UltimasAverias
    { get; set; } = new();
}

public class ConsumoResumenDashboard
{
    public string Nombre { get; set; } = string.Empty;

    public decimal MetrosCubicos { get; set; }
}

public class ConsumoAbonadoDashboard
{
    public int IdAbonado { get; set; }

    public string NIS { get; set; } = string.Empty;

    public string Sector { get; set; } = string.Empty;

    public string Tarifa { get; set; } = string.Empty;

    public int? CantidadPersonas { get; set; }

    public decimal MetrosCubicos { get; set; }
}
