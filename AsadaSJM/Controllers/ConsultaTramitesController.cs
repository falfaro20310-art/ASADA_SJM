using AsadaSJM.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace AsadaSJM.Controllers;

[Authorize]
public sealed class ConsultaTramitesController : Controller
{
    private readonly IConfiguration _configuration;

    public ConsultaTramitesController(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? busqueda)
    {
        var modelo =
            new ConsultaTramitesViewModel
            {
                Busqueda =
                    busqueda?.Trim()
                    ?? string.Empty
            };

        if (string.IsNullOrWhiteSpace(
            modelo.Busqueda))
        {
            return View(modelo);
        }

        modelo.BusquedaRealizada = true;

        string? idUsuarioClaim =
            User.FindFirstValue("IdUsuario");

        if (!int.TryParse(
            idUsuarioClaim,
            out int idUsuario))
        {
            ModelState.AddModelError(
                string.Empty,
                "No fue posible identificar al usuario autenticado.");

            return View(modelo);
        }

        string? connectionString =
            _configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrWhiteSpace(
            connectionString))
        {
            ModelState.AddModelError(
                string.Empty,
                "No fue posible conectar con la base de datos.");

            return View(modelo);
        }

        const string consulta = @"
            SELECT DISTINCT
                t.IdTramite,
                s.NumeroTramite,
                ISNULL(a.NumeroPaja, '') AS NumeroPaja,
                tt.Nombre AS TipoTramite,
                t.FechaSolicitud,
                t.Estado,
                t.FechaFinalizacion,
                ISNULL(t.Observaciones, '') AS MotivoRechazo
            FROM TRAMITE t
            INNER JOIN SOLICITUD_DISPONIBILIDAD_HIDRICA s
                ON s.IdTramite = t.IdTramite
            INNER JOIN TIPO_TRAMITE tt
                ON tt.IdTipoTramite = t.IdTipoTramite
            LEFT JOIN ABONADO a
                ON a.IdAbonado = t.IdAbonado
            WHERE
                t.IdUsuario = @IdUsuario
                AND
                (
                    s.NumeroTramite = @Busqueda
                    OR a.NumeroPaja = @Busqueda
                )
            ORDER BY
                t.FechaSolicitud DESC,
                t.IdTramite DESC;";

        await using var connection =
            new SqlConnection(
                connectionString);

        await using var command =
            new SqlCommand(
                consulta,
                connection);

        command.Parameters.AddWithValue(
            "@IdUsuario",
            idUsuario);

        command.Parameters.AddWithValue(
            "@Busqueda",
            modelo.Busqueda);

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            modelo.Resultados.Add(
                new ConsultaTramiteResultadoViewModel
                {
                    IdTramite =
                        Convert.ToInt32(
                            reader["IdTramite"]),

                    NumeroTramite =
                        reader["NumeroTramite"]?.ToString()
                        ?? string.Empty,

                    NumeroPaja =
                        reader["NumeroPaja"]?.ToString()
                        ?? string.Empty,

                    TipoTramite =
                        reader["TipoTramite"]?.ToString()
                        ?? string.Empty,

                    FechaSolicitud =
                        Convert.ToDateTime(
                            reader["FechaSolicitud"]),

                    Estado =
                        reader["Estado"]?.ToString()
                        ?? string.Empty,

                    FechaFinalizacion =
                        reader["FechaFinalizacion"]
                            == DBNull.Value
                            ? null
                            : Convert.ToDateTime(
                                reader["FechaFinalizacion"]),

                    MotivoRechazo =
                        reader["MotivoRechazo"]?.ToString()
                        ?? string.Empty
                });
        }

        return View(modelo);
    }
}
