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
    private readonly IWebHostEnvironment _environment;

    public ConsultaTramitesController(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
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

        int? idUsuario =
            ObtenerIdUsuarioActual();

        if (idUsuario == null)
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

        await using var connection =
            new SqlConnection(
                connectionString);

        await connection.OpenAsync();

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

        await using (
            var command =
                new SqlCommand(
                    consulta,
                    connection))
        {
            command.Parameters.AddWithValue(
                "@IdUsuario",
                idUsuario.Value);

            command.Parameters.AddWithValue(
                "@Busqueda",
                modelo.Busqueda);

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
        }

        if (modelo.Resultados.Count > 0)
        {
            await CargarDocumentosFinalesAsync(
                connection,
                idUsuario.Value,
                modelo.Busqueda,
                modelo.Resultados);
        }

        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> DescargarDocumentoFinal(
        int id)
    {
        int? idUsuario =
            ObtenerIdUsuarioActual();

        if (idUsuario == null)
        {
            return Forbid();
        }

        string? connectionString =
            _configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrWhiteSpace(
            connectionString))
        {
            return NotFound();
        }

        const string consulta = @"
            SELECT TOP 1
                td.NombreArchivo,
                td.RutaArchivo
            FROM TRAMITE_DOCUMENTO td
            INNER JOIN TRAMITE t
                ON t.IdTramite = td.IdTramite
            WHERE
                td.IdDocumento = @IdDocumento
                AND t.IdUsuario = @IdUsuario
                AND td.TipoDocumento =
                    'ConstanciaDisponibilidadHidrica';";

        await using var connection =
            new SqlConnection(
                connectionString);

        await using var command =
            new SqlCommand(
                consulta,
                connection);

        command.Parameters.AddWithValue(
            "@IdDocumento",
            id);

        command.Parameters.AddWithValue(
            "@IdUsuario",
            idUsuario.Value);

        await connection.OpenAsync();

        string? nombreArchivo = null;
        string? rutaRegistrada = null;

        await using (
            var reader =
                await command.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                nombreArchivo =
                    reader["NombreArchivo"]?.ToString();

                rutaRegistrada =
                    reader["RutaArchivo"]?.ToString();
            }
        }

        if (string.IsNullOrWhiteSpace(
                rutaRegistrada))
        {
            return NotFound();
        }

        string raiz =
            Path.GetFullPath(
                _environment.ContentRootPath);

        string rutaFisica =
            Path.GetFullPath(
                Path.Combine(
                    raiz,
                    rutaRegistrada.Replace(
                        '/',
                        Path.DirectorySeparatorChar)));

        string raizConSeparador =
            raiz.EndsWith(
                Path.DirectorySeparatorChar)
                ? raiz
                : raiz + Path.DirectorySeparatorChar;

        if (!rutaFisica.StartsWith(
                raizConSeparador,
                StringComparison.OrdinalIgnoreCase) ||
            !System.IO.File.Exists(
                rutaFisica))
        {
            return NotFound();
        }

        byte[] contenido =
            await System.IO.File.ReadAllBytesAsync(
                rutaFisica);

        string nombreDescarga =
            string.IsNullOrWhiteSpace(
                nombreArchivo)
                ? $"DocumentoFinal_{id}.pdf"
                : nombreArchivo;

        return File(
            contenido,
            "application/pdf",
            nombreDescarga);
    }

    private static async Task CargarDocumentosFinalesAsync(
        SqlConnection connection,
        int idUsuario,
        string busqueda,
        List<ConsultaTramiteResultadoViewModel> resultados)
    {
        const string consulta = @"
            SELECT DISTINCT
                td.IdDocumento,
                td.IdTramite,
                td.NombreArchivo,
                td.FechaCarga
            FROM TRAMITE_DOCUMENTO td
            INNER JOIN TRAMITE t
                ON t.IdTramite = td.IdTramite
            INNER JOIN SOLICITUD_DISPONIBILIDAD_HIDRICA s
                ON s.IdTramite = t.IdTramite
            LEFT JOIN ABONADO a
                ON a.IdAbonado = t.IdAbonado
            WHERE
                t.IdUsuario = @IdUsuario
                AND td.TipoDocumento =
                    'ConstanciaDisponibilidadHidrica'
                AND
                (
                    s.NumeroTramite = @Busqueda
                    OR a.NumeroPaja = @Busqueda
                )
            ORDER BY
                td.FechaCarga DESC,
                td.IdDocumento DESC;";

        await using var command =
            new SqlCommand(
                consulta,
                connection);

        command.Parameters.AddWithValue(
            "@IdUsuario",
            idUsuario);

        command.Parameters.AddWithValue(
            "@Busqueda",
            busqueda);

        var indice =
            resultados.ToDictionary(
                x => x.IdTramite);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            int idTramite =
                Convert.ToInt32(
                    reader["IdTramite"]);

            if (!indice.TryGetValue(
                idTramite,
                out var tramite))
            {
                continue;
            }

            tramite.DocumentosFinales.Add(
                new DocumentoFinalTramiteViewModel
                {
                    IdDocumento =
                        Convert.ToInt32(
                            reader["IdDocumento"]),

                    NombreArchivo =
                        reader["NombreArchivo"]?.ToString()
                        ?? "Documento final",

                    FechaCarga =
                        Convert.ToDateTime(
                            reader["FechaCarga"])
                });
        }
    }

    private int? ObtenerIdUsuarioActual()
    {
        string? idUsuarioClaim =
            User.FindFirstValue(
                "IdUsuario");

        return int.TryParse(
            idUsuarioClaim,
            out int idUsuario)
                ? idUsuario
                : null;
    }
}
