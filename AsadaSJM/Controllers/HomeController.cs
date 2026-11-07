
using AsadaSJM.Data;
using AsadaSJM.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AsadaSJM.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    // MPA-01: Panel administrativo.
    // Únicamente consulta datos; no altera otras entidades.
    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public async Task<IActionResult> Index(int? ano, int? mes)
    {
        var hoy = DateTime.Today;
        var anioSeleccionado = ano ?? hoy.Year;

        if (anioSeleccionado < 2000 || anioSeleccionado > 2100 ||
            (mes.HasValue && (mes.Value < 1 || mes.Value > 12)))
        {
            return BadRequest("El año o el mes seleccionado no es válido.");
        }

        var inicio = new DateTime(anioSeleccionado, mes ?? 1, 1);

        var finExclusivo = mes.HasValue
            ? inicio.AddMonths(1)
            : inicio.AddYears(1);

        var modelo = new PanelAdministrativoDashboardViewModel
        {
            Ano = anioSeleccionado,
            Mes = mes,
            InicioPeriodo = inicio,
            FinPeriodoExclusivo = finExclusivo,

            // Se conservan los indicadores y el listado existentes.
            AveriasRegistradas = await _context.Averias.CountAsync(),

            TramitesPendientes = await _context.Tramites
                .CountAsync(t => t.Estado == "Pendiente"),

            UltimasAverias = await _context.Averias
                .Include(a => a.Abonado)
                .OrderByDescending(a => a.FechaReporte)
                .Take(5)
                .ToListAsync()
        };

        // Consultas ADO.NET aisladas.
        // No se cambian Abonado, Recibo ni ApplicationDbContext.
        var cadenaConexion = _context.Database.GetConnectionString();

        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException(
                "No se encontró la conexión SQL Server configurada.");
        }

        await using var conexion = new SqlConnection(cadenaConexion);
        await conexion.OpenAsync();

        // Abonados actuales y registrados durante el período.
        await using (var comando = new SqlCommand(@"
            SELECT
                COUNT(*) AS TotalAbonados,
                COALESCE(SUM(
                    CASE
                        WHEN FechaRegistro >= @Inicio
                         AND FechaRegistro < @Fin
                        THEN 1 ELSE 0
                    END
                ), 0) AS Nuevos,
                COALESCE(SUM(
                    CASE WHEN FechaRegistro IS NULL
                    THEN 1 ELSE 0 END
                ), 0) AS SinFecha
            FROM dbo.ABONADO;", conexion))
        {
            AgregarPeriodo(comando, inicio, finExclusivo);

            await using var lector = await comando.ExecuteReaderAsync();

            if (await lector.ReadAsync())
            {
                modelo.TotalAbonadosActuales = lector.GetInt32(0);

                modelo.AbonadosRegistradosPeriodo =
                    Convert.ToInt32(lector.GetValue(1));

                modelo.AbonadosSinFechaRegistro =
                    Convert.ToInt32(lector.GetValue(2));
            }
        }

        // Consumos por abonado.
        await using (var comando = new SqlCommand(@"
            SELECT
                a.IdAbonado,
                a.NIS,
                COALESCE(s.Nombre, 'Sin sector') AS Sector,
                COALESCE(t.Nombre, 'Sin tarifa') AS Tarifa,
                a.CantidadPersonas,
                SUM(c.MetrosCubicos) AS TotalMetrosCubicos
            FROM dbo.CONSUMO AS c
            INNER JOIN dbo.ABONADO AS a
                ON a.IdAbonado = c.IdAbonado
            LEFT JOIN dbo.SECTOR AS s
                ON s.IdSector = a.IdSector
            LEFT JOIN dbo.TARIFA AS t
                ON t.IdTarifa = a.IdTarifa
            WHERE c.FechaLectura >= @Inicio
              AND c.FechaLectura < @Fin
            GROUP BY
                a.IdAbonado,
                a.NIS,
                s.Nombre,
                t.Nombre,
                a.CantidadPersonas
            ORDER BY
                SUM(c.MetrosCubicos) DESC,
                a.NIS;", conexion))
        {
            AgregarPeriodo(comando, inicio, finExclusivo);

            await using var lector = await comando.ExecuteReaderAsync();

            while (await lector.ReadAsync())
            {
                modelo.ConsumoPorAbonado.Add(
                    new ConsumoAbonadoDashboard
                    {
                        IdAbonado = lector.GetInt32(0),
                        NIS = lector.GetString(1),
                        Sector = lector.GetString(2),
                        Tarifa = lector.GetString(3),

                        CantidadPersonas = lector.IsDBNull(4)
                            ? null
                            : lector.GetInt32(4),

                        MetrosCubicos = lector.GetDecimal(5)
                    });
            }
        }

        modelo.ConsumoTotalM3 = modelo.ConsumoPorAbonado
            .Sum(x => x.MetrosCubicos);

        // Agrupación por sector.
        modelo.ConsumoPorSector = modelo.ConsumoPorAbonado
            .GroupBy(x => x.Sector)
            .Select(g => new ConsumoResumenDashboard
            {
                Nombre = g.Key,
                MetrosCubicos = g.Sum(x => x.MetrosCubicos)
            })
            .OrderByDescending(x => x.MetrosCubicos)
            .ToList();

        // Agrupación por tarifa.
        modelo.ConsumoPorTarifa = modelo.ConsumoPorAbonado
            .GroupBy(x => x.Tarifa)
            .Select(g => new ConsumoResumenDashboard
            {
                Nombre = g.Key,
                MetrosCubicos = g.Sum(x => x.MetrosCubicos)
            })
            .OrderByDescending(x => x.MetrosCubicos)
            .ToList();

        // Litros por persona por día.
        var finDeDias = finExclusivo < hoy.AddDays(1)
            ? finExclusivo
            : hoy.AddDays(1);

        var dias = Math.Max(0, (finDeDias - inicio).Days);

        var conPoblacion = modelo.ConsumoPorAbonado
            .Where(x =>
                x.CantidadPersonas.HasValue &&
                x.CantidadPersonas.Value > 0)
            .ToList();

        modelo.AbonadosConConsumoSinCantidadPersonas =
            modelo.ConsumoPorAbonado.Count(x =>
                !x.CantidadPersonas.HasValue ||
                x.CantidadPersonas.Value <= 0);

        var totalPersonas = conPoblacion
            .Sum(x => (long)x.CantidadPersonas!.Value);

        if (dias > 0 && totalPersonas > 0)
        {
            modelo.LitrosPorPersonaDia =
                conPoblacion.Sum(x => x.MetrosCubicos)
                * 1000m / (totalPersonas * dias);
        }

        // Índice estimado de mora.
        // Los estados de pago deben validarse antes de
        // considerar este indicador como definitivo.
        var corte = finExclusivo < hoy.AddDays(1)
            ? finExclusivo
            : hoy.AddDays(1);

        await using (var comando = new SqlCommand(@"
            SELECT
                COUNT(*) AS TotalRecibos,
                COALESCE(SUM(
                    CASE
                        WHEN r.FechaVencimiento < @Corte
                         AND r.Monto > COALESCE(p.TotalPagado, 0)
                        THEN 1 ELSE 0
                    END
                ), 0) AS VencidosPendientes
            FROM dbo.RECIBO AS r
            OUTER APPLY
            (
                SELECT SUM(pg.Monto) AS TotalPagado
                FROM dbo.PAGO AS pg
                WHERE pg.IdRecibo = r.IdRecibo
                  AND pg.FechaPago < @Corte
            ) AS p
            WHERE r.FechaEmision >= @Inicio
              AND r.FechaEmision < @Fin;", conexion))
        {
            AgregarPeriodo(comando, inicio, finExclusivo);

            comando.Parameters.Add("@Corte", SqlDbType.DateTime2)
                .Value = corte;

            await using var lector = await comando.ExecuteReaderAsync();

            if (await lector.ReadAsync())
            {
                modelo.RecibosEmitidosPeriodo =
                    lector.GetInt32(0);

                modelo.RecibosVencidosPendientes =
                    Convert.ToInt32(lector.GetValue(1));
            }
        }

        if (modelo.RecibosEmitidosPeriodo > 0)
        {
            modelo.IndiceMoraPorcentaje =
                100m * modelo.RecibosVencidosPendientes
                / modelo.RecibosEmitidosPeriodo;
        }

        return View(modelo);
    }

    private static void AgregarPeriodo(
        SqlCommand comando,
        DateTime inicio,
        DateTime fin)
    {
        comando.Parameters.Add("@Inicio", SqlDbType.DateTime2)
            .Value = inicio;

        comando.Parameters.Add("@Fin", SqlDbType.DateTime2)
            .Value = fin;
    }

    [AllowAnonymous]
    public IActionResult Error()
    {
        return View();
    }
}
