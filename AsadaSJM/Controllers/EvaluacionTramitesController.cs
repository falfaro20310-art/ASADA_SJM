using AsadaSJM.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text;

namespace AsadaSJM.Controllers;

[Authorize(Roles = "Administrador")]
public class EvaluacionTramitesController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public EvaluacionTramitesController(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }


    // =========================================================
    // LISTADO DE SOLICITUDES DE DISPONIBILIDAD HÍDRICA
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var tramites =
            new List<EvaluacionTramiteListaViewModel>();

        string? connectionString =
            _configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return View(tramites);
        }

        using SqlConnection connection =
            new SqlConnection(connectionString);

        const string consulta = @"
            SELECT
                t.IdTramite,
                s.NumeroTramite,
                s.NombreSolicitante,
                t.FechaSolicitud,
                t.Estado,
                s.EsSegregacionFraccionamiento
            FROM TRAMITE t
            INNER JOIN SOLICITUD_DISPONIBILIDAD_HIDRICA s
                ON s.IdTramite = t.IdTramite
            ORDER BY t.FechaSolicitud DESC,
                     t.IdTramite DESC;";

        using SqlCommand comando =
            new SqlCommand(
                consulta,
                connection);

        await connection.OpenAsync();

        await using SqlDataReader lector =
            await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            tramites.Add(
                new EvaluacionTramiteListaViewModel
                {
                    IdTramite =
                        Convert.ToInt32(
                            lector["IdTramite"]),

                    NumeroTramite =
                        lector["NumeroTramite"]?.ToString()
                        ?? string.Empty,

                    NombreSolicitante =
                        lector["NombreSolicitante"]?.ToString()
                        ?? string.Empty,

                    FechaSolicitud =
                        Convert.ToDateTime(
                            lector["FechaSolicitud"]),

                    Estado =
                        lector["Estado"]?.ToString()
                        ?? string.Empty,

                    EsSegregacionFraccionamiento =
                        Convert.ToBoolean(
                            lector["EsSegregacionFraccionamiento"])
                });
        }

        return View(tramites);
    }


    // =========================================================
    // DETALLE / EVALUACIÓN
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Detalle(
        int id)
    {
        EvaluacionTramiteViewModel? modelo =
            await CargarTramiteAsync(id);

        if (modelo == null)
        {
            return NotFound();
        }

        return View(modelo);
    }


    // =========================================================
    // DESCARGAR CONSTANCIA PDF
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> DescargarConstancia(
        int id)
    {
        string? connectionString =
            _configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrWhiteSpace(
            connectionString))
        {
            return NotFound();
        }

        using SqlConnection connection =
            new SqlConnection(
                connectionString);

        const string consulta = @"
            SELECT
                e.RutaConstancia,
                s.NumeroTramite
            FROM EVALUACION_DISPONIBILIDAD_HIDRICA e
            INNER JOIN SOLICITUD_DISPONIBILIDAD_HIDRICA s
                ON s.IdTramite = e.IdTramite
            WHERE
                e.IdTramite = @IdTramite
                AND e.EstadoEvaluacion = 'Aprobado'
                AND e.RutaConstancia IS NOT NULL;";

        using SqlCommand comando =
            new SqlCommand(
                consulta,
                connection);

        comando.Parameters.AddWithValue(
            "@IdTramite",
            id);

        await connection.OpenAsync();

        string? rutaRegistrada = null;
        string? numeroTramite = null;

        await using (
            SqlDataReader lector =
                await comando.ExecuteReaderAsync())
        {
            if (await lector.ReadAsync())
            {
                rutaRegistrada =
                    lector["RutaConstancia"]?.ToString();

                numeroTramite =
                    lector["NumeroTramite"]?.ToString();
            }
        }

        if (string.IsNullOrWhiteSpace(
                rutaRegistrada))
        {
            return NotFound();
        }

        string rutaFisica =
            Path.Combine(
                _environment.ContentRootPath,
                rutaRegistrada.Replace(
                    '/',
                    Path.DirectorySeparatorChar));

        if (!System.IO.File.Exists(
                rutaFisica))
        {
            return NotFound();
        }

        byte[] contenido =
            await System.IO.File.ReadAllBytesAsync(
                rutaFisica);

        string nombreDescarga =
            $"Constancia_{numeroTramite ?? id.ToString()}.pdf";

        return File(
            contenido,
            "application/pdf",
            nombreDescarga);
    }


    // =========================================================
    // APROBAR
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aprobar(
        EvaluacionTramiteViewModel modelo)
    {
        EvaluacionTramiteViewModel? actual =
            await CargarTramiteAsync(
                modelo.IdTramite);

        if (actual == null)
        {
            return NotFound();
        }

        CopiarChecklist(
            modelo,
            actual);

        if (!modelo.TodosRequisitosCumplidos)
        {
            TempData["ErrorEvaluacion"] =
                "Para aprobar la solicitud deben cumplirse los 9 requisitos aplicables.";

            return View(
                "Detalle",
                CombinarEvaluacion(
                    actual,
                    modelo));
        }

        if (actual.Documentos.Any(
            d => d.EstaVencido))
        {
            TempData["ErrorEvaluacion"] =
                "No se puede aprobar el trámite mientras existan documentos vencidos.";

            return View(
                "Detalle",
                CombinarEvaluacion(
                    actual,
                    modelo));
        }

        int? idUsuario =
            ObtenerIdUsuarioActual();

        if (idUsuario == null)
        {
            TempData["ErrorEvaluacion"] =
                "No fue posible identificar al usuario evaluador.";

            return View(
                "Detalle",
                CombinarEvaluacion(
                    actual,
                    modelo));
        }

        bool resultado =
            await GuardarEvaluacionAsync(
                actual,
                modelo,
                idUsuario.Value,
                "Aprobado",
                null);

        if (!resultado)
        {
            TempData["ErrorEvaluacion"] =
                "No fue posible aprobar el trámite.";

            return RedirectToAction(
                nameof(Detalle),
                new { id = modelo.IdTramite });
        }

        TempData["MensajeEvaluacion"] =
            "El trámite fue aprobado correctamente.";

        return RedirectToAction(
            nameof(Detalle),
            new { id = modelo.IdTramite });
    }


    // =========================================================
    // RECHAZAR
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(
        EvaluacionTramiteViewModel modelo)
    {
        EvaluacionTramiteViewModel? actual =
            await CargarTramiteAsync(
                modelo.IdTramite);

        if (actual == null)
        {
            return NotFound();
        }

        CopiarChecklist(
            modelo,
            actual);

        if (string.IsNullOrWhiteSpace(
            modelo.MotivoRechazo))
        {
            TempData["ErrorEvaluacion"] =
                "Debe indicar el motivo del rechazo.";

            return View(
                "Detalle",
                CombinarEvaluacion(
                    actual,
                    modelo));
        }

        int? idUsuario =
            ObtenerIdUsuarioActual();

        if (idUsuario == null)
        {
            TempData["ErrorEvaluacion"] =
                "No fue posible identificar al usuario evaluador.";

            return View(
                "Detalle",
                CombinarEvaluacion(
                    actual,
                    modelo));
        }

        bool resultado =
            await GuardarEvaluacionAsync(
                actual,
                modelo,
                idUsuario.Value,
                "Rechazado",
                modelo.MotivoRechazo.Trim());

        if (!resultado)
        {
            TempData["ErrorEvaluacion"] =
                "No fue posible rechazar el trámite.";

            return RedirectToAction(
                nameof(Detalle),
                new { id = modelo.IdTramite });
        }

        TempData["MensajeEvaluacion"] =
            "El trámite fue rechazado correctamente.";

        return RedirectToAction(
            nameof(Detalle),
            new { id = modelo.IdTramite });
    }


    // =========================================================
    // CARGAR TRÁMITE
    // =========================================================

    private async Task<EvaluacionTramiteViewModel?>
        CargarTramiteAsync(
            int idTramite)
    {
        string? connectionString =
            _configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrWhiteSpace(
            connectionString))
        {
            return null;
        }

        using SqlConnection connection =
            new SqlConnection(
                connectionString);

        await connection.OpenAsync();

        const string consulta = @"
            SELECT
                t.IdTramite,
                s.NumeroTramite,
                s.NombreSolicitante,
                s.PropositoDisponibilidad,
                s.DescripcionProyecto,
                s.EsPropietarioRegistral,
                s.EsSegregacionFraccionamiento,
                t.FechaSolicitud,
                t.Estado
            FROM TRAMITE t
            INNER JOIN SOLICITUD_DISPONIBILIDAD_HIDRICA s
                ON s.IdTramite = t.IdTramite
            WHERE t.IdTramite = @IdTramite;";

        using SqlCommand comando =
            new SqlCommand(
                consulta,
                connection);

        comando.Parameters.AddWithValue(
            "@IdTramite",
            idTramite);

        EvaluacionTramiteViewModel? modelo = null;

        await using (
            SqlDataReader lector =
                await comando.ExecuteReaderAsync())
        {
            if (await lector.ReadAsync())
            {
                modelo =
                    new EvaluacionTramiteViewModel
                    {
                        IdTramite =
                            Convert.ToInt32(
                                lector["IdTramite"]),

                        NumeroTramite =
                            lector["NumeroTramite"]?.ToString()
                            ?? string.Empty,

                        NombreSolicitante =
                            lector["NombreSolicitante"]?.ToString()
                            ?? string.Empty,

                        PropositoDisponibilidad =
                            lector["PropositoDisponibilidad"]?.ToString()
                            ?? string.Empty,

                        DescripcionProyecto =
                            lector["DescripcionProyecto"]?.ToString()
                            ?? string.Empty,

                        EsPropietarioRegistral =
                            Convert.ToBoolean(
                                lector["EsPropietarioRegistral"]),

                        EsSegregacionFraccionamiento =
                            Convert.ToBoolean(
                                lector["EsSegregacionFraccionamiento"]),

                        FechaSolicitud =
                            Convert.ToDateTime(
                                lector["FechaSolicitud"]),

                        Estado =
                            lector["Estado"]?.ToString()
                            ?? string.Empty
                    };
            }
        }

        if (modelo == null)
        {
            return null;
        }

        const string consultaDocumentos = @"
            SELECT
                IdDocumento,
                TipoDocumento,
                NombreArchivo,
                RutaArchivo,
                FechaEmision,
                VigenciaDias
            FROM TRAMITE_DOCUMENTO
            WHERE IdTramite = @IdTramite
            ORDER BY IdDocumento;";

        using SqlCommand comandoDocumentos =
            new SqlCommand(
                consultaDocumentos,
                connection);

        comandoDocumentos.Parameters.AddWithValue(
            "@IdTramite",
            idTramite);

        await using (
            SqlDataReader lectorDocumentos =
                await comandoDocumentos.ExecuteReaderAsync())
        {
            while (await lectorDocumentos.ReadAsync())
            {
                modelo.Documentos.Add(
                    new DocumentoEvaluacionViewModel
                    {
                        IdDocumento =
                            Convert.ToInt32(
                                lectorDocumentos["IdDocumento"]),

                        TipoDocumento =
                            lectorDocumentos["TipoDocumento"] == DBNull.Value
                                ? "Sin clasificar"
                                : lectorDocumentos["TipoDocumento"]?.ToString()
                                  ?? "Sin clasificar",

                        NombreArchivo =
                            lectorDocumentos["NombreArchivo"]?.ToString()
                            ?? string.Empty,

                        RutaArchivo =
                            lectorDocumentos["RutaArchivo"]?.ToString()
                            ?? string.Empty,

                        FechaEmision =
                            lectorDocumentos["FechaEmision"] == DBNull.Value
                                ? null
                                : Convert.ToDateTime(
                                    lectorDocumentos["FechaEmision"]),

                        VigenciaDias =
                            lectorDocumentos["VigenciaDias"] == DBNull.Value
                                ? null
                                : Convert.ToInt32(
                                    lectorDocumentos["VigenciaDias"])
                    });
            }
        }

        const string consultaEvaluacion = @"
            SELECT
                Requisito1,
                Requisito2,
                Requisito3,
                Requisito4,
                Requisito5,
                Requisito6,
                Requisito7,
                Requisito8,
                Requisito9,
                MotivoRechazo
            FROM EVALUACION_DISPONIBILIDAD_HIDRICA
            WHERE IdTramite = @IdTramite;";

        using SqlCommand comandoEvaluacion =
            new SqlCommand(
                consultaEvaluacion,
                connection);

        comandoEvaluacion.Parameters.AddWithValue(
            "@IdTramite",
            idTramite);

        await using (
            SqlDataReader lectorEvaluacion =
                await comandoEvaluacion.ExecuteReaderAsync())
        {
            if (await lectorEvaluacion.ReadAsync())
            {
                modelo.Requisito1 =
                    Convert.ToBoolean(
                        lectorEvaluacion["Requisito1"]);

                modelo.Requisito2 =
                    Convert.ToBoolean(
                        lectorEvaluacion["Requisito2"]);

                modelo.Requisito3 =
                    Convert.ToBoolean(
                        lectorEvaluacion["Requisito3"]);

                modelo.Requisito4 =
                    Convert.ToBoolean(
                        lectorEvaluacion["Requisito4"]);

                modelo.Requisito5 =
                    Convert.ToBoolean(
                        lectorEvaluacion["Requisito5"]);

                modelo.Requisito6 =
                    Convert.ToBoolean(
                        lectorEvaluacion["Requisito6"]);

                modelo.Requisito7 =
                    Convert.ToBoolean(
                        lectorEvaluacion["Requisito7"]);

                modelo.Requisito8 =
                    Convert.ToBoolean(
                        lectorEvaluacion["Requisito8"]);

                modelo.Requisito9 =
                    Convert.ToBoolean(
                        lectorEvaluacion["Requisito9"]);

                modelo.MotivoRechazo =
                    lectorEvaluacion["MotivoRechazo"]
                        == DBNull.Value
                        ? null
                        : lectorEvaluacion["MotivoRechazo"]
                            ?.ToString();
            }
        }

        return modelo;
    }


    // =========================================================
    // GUARDAR EVALUACIÓN + CAMBIO DE ESTADO
    // =========================================================

    private async Task<bool> GuardarEvaluacionAsync(
        EvaluacionTramiteViewModel actual,
        EvaluacionTramiteViewModel evaluacion,
        int idUsuarioEvaluador,
        string nuevoEstado,
        string? motivoRechazo)
    {
        string? connectionString =
            _configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrWhiteSpace(
            connectionString))
        {
            return false;
        }

        string? rutaConstanciaRegistrada = null;
        string? rutaConstanciaFisica = null;

        if (nuevoEstado == "Aprobado")
        {
            var constancia =
                await GenerarConstanciaPdfAsync(
                    actual);

            rutaConstanciaRegistrada =
                constancia.RutaRegistrada;

            rutaConstanciaFisica =
                constancia.RutaFisica;
        }

        using SqlConnection connection =
            new SqlConnection(
                connectionString);

        await connection.OpenAsync();

        using SqlTransaction transaction =
            connection.BeginTransaction();

        try
        {
            const string guardarEvaluacion = @"
                IF EXISTS
                (
                    SELECT 1
                    FROM EVALUACION_DISPONIBILIDAD_HIDRICA
                    WHERE IdTramite = @IdTramite
                )
                BEGIN
                    UPDATE EVALUACION_DISPONIBILIDAD_HIDRICA
                    SET
                        IdUsuarioEvaluador = @IdUsuarioEvaluador,
                        Requisito1 = @Requisito1,
                        Requisito2 = @Requisito2,
                        Requisito3 = @Requisito3,
                        Requisito4 = @Requisito4,
                        Requisito5 = @Requisito5,
                        Requisito6 = @Requisito6,
                        Requisito7 = @Requisito7,
                        Requisito8 = @Requisito8,
                        Requisito9 = @Requisito9,
                        EstadoEvaluacion = @EstadoEvaluacion,
                        MotivoRechazo = @MotivoRechazo,
                        RutaConstancia = @RutaConstancia,
                        FechaEvaluacion = GETDATE()
                    WHERE IdTramite = @IdTramite;
                END
                ELSE
                BEGIN
                    INSERT INTO EVALUACION_DISPONIBILIDAD_HIDRICA
                    (
                        IdTramite,
                        IdUsuarioEvaluador,
                        Requisito1,
                        Requisito2,
                        Requisito3,
                        Requisito4,
                        Requisito5,
                        Requisito6,
                        Requisito7,
                        Requisito8,
                        Requisito9,
                        EstadoEvaluacion,
                        MotivoRechazo,
                        RutaConstancia,
                        FechaEvaluacion
                    )
                    VALUES
                    (
                        @IdTramite,
                        @IdUsuarioEvaluador,
                        @Requisito1,
                        @Requisito2,
                        @Requisito3,
                        @Requisito4,
                        @Requisito5,
                        @Requisito6,
                        @Requisito7,
                        @Requisito8,
                        @Requisito9,
                        @EstadoEvaluacion,
                        @MotivoRechazo,
                        @RutaConstancia,
                        GETDATE()
                    );
                END;";

            using SqlCommand comandoEvaluacion =
                new SqlCommand(
                    guardarEvaluacion,
                    connection,
                    transaction);

            comandoEvaluacion.Parameters.AddWithValue(
                "@IdTramite",
                actual.IdTramite);

            comandoEvaluacion.Parameters.AddWithValue(
                "@IdUsuarioEvaluador",
                idUsuarioEvaluador);

            comandoEvaluacion.Parameters.AddWithValue(
                "@Requisito1",
                evaluacion.Requisito1);

            comandoEvaluacion.Parameters.AddWithValue(
                "@Requisito2",
                evaluacion.Requisito2);

            comandoEvaluacion.Parameters.AddWithValue(
                "@Requisito3",
                evaluacion.Requisito3);

            comandoEvaluacion.Parameters.AddWithValue(
                "@Requisito4",
                evaluacion.Requisito4);

            comandoEvaluacion.Parameters.AddWithValue(
                "@Requisito5",
                evaluacion.Requisito5);

            comandoEvaluacion.Parameters.AddWithValue(
                "@Requisito6",
                evaluacion.Requisito6);

            comandoEvaluacion.Parameters.AddWithValue(
                "@Requisito7",
                evaluacion.Requisito7);

            comandoEvaluacion.Parameters.AddWithValue(
                "@Requisito8",
                evaluacion.Requisito8);

            comandoEvaluacion.Parameters.AddWithValue(
                "@Requisito9",
                evaluacion.Requisito9);

            comandoEvaluacion.Parameters.AddWithValue(
                "@EstadoEvaluacion",
                nuevoEstado);

            comandoEvaluacion.Parameters.AddWithValue(
                "@MotivoRechazo",
                (object?)motivoRechazo
                ?? DBNull.Value);

            comandoEvaluacion.Parameters.AddWithValue(
                "@RutaConstancia",
                (object?)rutaConstanciaRegistrada
                ?? DBNull.Value);

            await comandoEvaluacion
                .ExecuteNonQueryAsync();


            if (nuevoEstado == "Aprobado" &&
                !string.IsNullOrWhiteSpace(
                    rutaConstanciaRegistrada))
            {
                const string registrarConstancia = @"
                    IF NOT EXISTS
                    (
                        SELECT 1
                        FROM TRAMITE_DOCUMENTO
                        WHERE
                            IdTramite = @IdTramite
                            AND TipoDocumento =
                                'ConstanciaDisponibilidadHidrica'
                    )
                    BEGIN
                        INSERT INTO TRAMITE_DOCUMENTO
                        (
                            IdTramite,
                            NombreArchivo,
                            RutaArchivo,
                            FechaCarga,
                            TipoDocumento,
                            FechaEmision,
                            VigenciaDias
                        )
                        VALUES
                        (
                            @IdTramite,
                            @NombreArchivo,
                            @RutaArchivo,
                            GETDATE(),
                            'ConstanciaDisponibilidadHidrica',
                            CAST(GETDATE() AS DATE),
                            NULL
                        );
                    END;";

                using SqlCommand comandoConstancia =
                    new SqlCommand(
                        registrarConstancia,
                        connection,
                        transaction);

                comandoConstancia.Parameters.AddWithValue(
                    "@IdTramite",
                    actual.IdTramite);

                comandoConstancia.Parameters.AddWithValue(
                    "@NombreArchivo",
                    $"Constancia de Disponibilidad Hídrica - {actual.NumeroTramite}.pdf");

                comandoConstancia.Parameters.AddWithValue(
                    "@RutaArchivo",
                    rutaConstanciaRegistrada);

                await comandoConstancia
                    .ExecuteNonQueryAsync();
            }


            const string actualizarTramite = @"
                UPDATE TRAMITE
                SET
                    Estado = @EstadoNuevo,
                    FechaFinalizacion = GETDATE(),
                    Observaciones = @Observaciones,
                    IdUsuarioResponsable = @IdUsuarioResponsable
                WHERE IdTramite = @IdTramite;";

            using SqlCommand comandoTramite =
                new SqlCommand(
                    actualizarTramite,
                    connection,
                    transaction);

            comandoTramite.Parameters.AddWithValue(
                "@EstadoNuevo",
                nuevoEstado);

            comandoTramite.Parameters.AddWithValue(
                "@Observaciones",
                (object?)motivoRechazo
                ?? DBNull.Value);

            comandoTramite.Parameters.AddWithValue(
                "@IdUsuarioResponsable",
                idUsuarioEvaluador);

            comandoTramite.Parameters.AddWithValue(
                "@IdTramite",
                actual.IdTramite);

            await comandoTramite
                .ExecuteNonQueryAsync();


            const string seguimiento = @"
                INSERT INTO TRAMITE_SEGUIMIENTO
                (
                    IdTramite,
                    IdUsuario,
                    Comentario,
                    EstadoAnterior,
                    EstadoNuevo,
                    Fecha
                )
                VALUES
                (
                    @IdTramite,
                    @IdUsuario,
                    @Comentario,
                    @EstadoAnterior,
                    @EstadoNuevo,
                    GETDATE()
                );";

            using SqlCommand comandoSeguimiento =
                new SqlCommand(
                    seguimiento,
                    connection,
                    transaction);

            string comentario =
                nuevoEstado == "Rechazado"
                    ? $"Trámite rechazado. Motivo: {motivoRechazo}"
                    : "Trámite aprobado tras completar la evaluación.";

            comandoSeguimiento.Parameters.AddWithValue(
                "@IdTramite",
                actual.IdTramite);

            comandoSeguimiento.Parameters.AddWithValue(
                "@IdUsuario",
                idUsuarioEvaluador);

            comandoSeguimiento.Parameters.AddWithValue(
                "@Comentario",
                comentario);

            comandoSeguimiento.Parameters.AddWithValue(
                "@EstadoAnterior",
                actual.Estado);

            comandoSeguimiento.Parameters.AddWithValue(
                "@EstadoNuevo",
                nuevoEstado);

            await comandoSeguimiento
                .ExecuteNonQueryAsync();

            transaction.Commit();

            return true;
        }
        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch
            {
            }

            if (!string.IsNullOrWhiteSpace(
                    rutaConstanciaFisica) &&
                System.IO.File.Exists(
                    rutaConstanciaFisica))
            {
                System.IO.File.Delete(
                    rutaConstanciaFisica);
            }

            return false;
        }
    }


    // =========================================================
    // GENERAR CONSTANCIA PDF
    // =========================================================

    private async Task<(
        string RutaRegistrada,
        string RutaFisica)>
        GenerarConstanciaPdfAsync(
            EvaluacionTramiteViewModel tramite)
    {
        string carpeta =
            Path.Combine(
                _environment.ContentRootPath,
                "App_Data",
                "tramites",
                tramite.IdTramite.ToString());

        Directory.CreateDirectory(
            carpeta);

        string nombreArchivo =
            $"constancia_disponibilidad_{tramite.IdTramite}.pdf";

        string rutaFisica =
            Path.Combine(
                carpeta,
                nombreArchivo);

        byte[] contenido =
            ConstruirConstanciaPdf(
                tramite);

        await System.IO.File.WriteAllBytesAsync(
            rutaFisica,
            contenido);

        string rutaRegistrada =
            $"App_Data/tramites/{tramite.IdTramite}/{nombreArchivo}";

        return (
            rutaRegistrada,
            rutaFisica);
    }


    private static byte[] ConstruirConstanciaPdf(
        EvaluacionTramiteViewModel tramite)
    {
        var contenido =
            new StringBuilder();

        AgregarTextoPdf(
            contenido,
            "ASADA SAN JOSE DE LA MONTANA",
            72,
            735,
            16,
            true);

        AgregarTextoPdf(
            contenido,
            "CONSTANCIA DE DISPONIBILIDAD HIDRICA",
            72,
            705,
            14,
            true);

        AgregarTextoPdf(
            contenido,
            $"Numero de tramite: {tramite.NumeroTramite}",
            72,
            665,
            11,
            true);

        AgregarTextoPdf(
            contenido,
            $"Fecha de emision: {DateTime.Today:dd/MM/yyyy}",
            72,
            645,
            11);

        AgregarTextoPdf(
            contenido,
            $"Solicitante: {tramite.NombreSolicitante}",
            72,
            615,
            11);

        float y = 580;

        foreach (string linea in DividirTexto(
            $"Proposito: {tramite.PropositoDisponibilidad}",
            78))
        {
            AgregarTextoPdf(
                contenido,
                linea,
                72,
                y,
                11);

            y -= 18;
        }

        y -= 10;

        foreach (string linea in DividirTexto(
            $"Descripcion del proyecto: {tramite.DescripcionProyecto}",
            78))
        {
            AgregarTextoPdf(
                contenido,
                linea,
                72,
                y,
                11);

            y -= 18;
        }

        y -= 25;

        foreach (string linea in DividirTexto(
            "Se hace constar que la solicitud indicada fue evaluada por la ASADA San Jose de la Montana y cumple con los nueve requisitos aplicables de la guia de disponibilidad hidrica.",
            82))
        {
            AgregarTextoPdf(
                contenido,
                linea,
                72,
                y,
                11);

            y -= 18;
        }

        y -= 15;

        foreach (string linea in DividirTexto(
            "Por lo anterior, el tramite se registra con estado APROBADO en el Sistema Web ASADA SJM.",
            82))
        {
            AgregarTextoPdf(
                contenido,
                linea,
                72,
                y,
                11);

            y -= 18;
        }

        AgregarTextoPdf(
            contenido,
            "Documento generado electronicamente por el Sistema Web ASADA SJM.",
            72,
            105,
            9);

        string streamTexto =
            contenido.ToString();

        byte[] streamBytes =
            Encoding.Latin1.GetBytes(
                streamTexto);

        var objetos =
            new List<byte[]>
            {
                Encoding.Latin1.GetBytes(
                    "<< /Type /Catalog /Pages 2 0 R >>"),

                Encoding.Latin1.GetBytes(
                    "<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),

                Encoding.Latin1.GetBytes(
                    "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>"),

                Encoding.Latin1.GetBytes(
                    "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"),

                Encoding.Latin1.GetBytes(
                    "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"),

                Encoding.Latin1.GetBytes(
                    $"<< /Length {streamBytes.Length} >>\nstream\n{streamTexto}endstream")
            };

        using var salida =
            new MemoryStream();

        void Escribir(
            string texto)
        {
            byte[] bytes =
                Encoding.Latin1.GetBytes(
                    texto);

            salida.Write(
                bytes,
                0,
                bytes.Length);
        }

        Escribir(
            "%PDF-1.4\n%ASADA-SJM\n");

        var offsets =
            new List<long>
            {
                0
            };

        for (int i = 0;
             i < objetos.Count;
             i++)
        {
            offsets.Add(
                salida.Position);

            Escribir(
                $"{i + 1} 0 obj\n");

            salida.Write(
                objetos[i],
                0,
                objetos[i].Length);

            Escribir(
                "\nendobj\n");
        }

        long inicioXref =
            salida.Position;

        Escribir(
            $"xref\n0 {objetos.Count + 1}\n");

        Escribir(
            "0000000000 65535 f \n");

        for (int i = 1;
             i < offsets.Count;
             i++)
        {
            Escribir(
                $"{offsets[i]:D10} 00000 n \n");
        }

        Escribir(
            $"trailer\n<< /Size {objetos.Count + 1} /Root 1 0 R >>\n");

        Escribir(
            $"startxref\n{inicioXref}\n%%EOF");

        return salida.ToArray();
    }


    private static void AgregarTextoPdf(
        StringBuilder contenido,
        string texto,
        float x,
        float y,
        int tamano,
        bool negrita = false)
    {
        string fuente =
            negrita
                ? "F2"
                : "F1";

        contenido.Append(
            $"BT /{fuente} {tamano} Tf {x:0.##} {y:0.##} Td ({EscaparTextoPdf(texto)}) Tj ET\n");
    }


    private static string EscaparTextoPdf(
        string texto)
    {
        return texto
            .Replace(
                "\\",
                "\\\\")
            .Replace(
                "(",
                "\\(")
            .Replace(
                ")",
                "\\)");
    }


    private static IEnumerable<string> DividirTexto(
        string texto,
        int maximoCaracteres)
    {
        string[] palabras =
            texto.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);

        var linea =
            new StringBuilder();

        foreach (string palabra in palabras)
        {
            if (linea.Length == 0)
            {
                linea.Append(
                    palabra);

                continue;
            }

            if (linea.Length +
                palabra.Length +
                1 >
                maximoCaracteres)
            {
                yield return
                    linea.ToString();

                linea.Clear();

                linea.Append(
                    palabra);
            }
            else
            {
                linea.Append(' ');
                linea.Append(
                    palabra);
            }
        }

        if (linea.Length > 0)
        {
            yield return
                linea.ToString();
        }
    }


    // =========================================================
    // HELPERS
    // =========================================================

    private int? ObtenerIdUsuarioActual()
    {
        string? valorIdUsuario =
            User.FindFirst(
                "IdUsuario")?.Value;

        if (string.IsNullOrWhiteSpace(
            valorIdUsuario))
        {
            return null;
        }

        return int.TryParse(
            valorIdUsuario,
            out int idUsuario)
            ? idUsuario
            : null;
    }


    private static void CopiarChecklist(
        EvaluacionTramiteViewModel origen,
        EvaluacionTramiteViewModel destino)
    {
        destino.Requisito1 = origen.Requisito1;
        destino.Requisito2 = origen.Requisito2;
        destino.Requisito3 = origen.Requisito3;
        destino.Requisito4 = origen.Requisito4;
        destino.Requisito5 = origen.Requisito5;
        destino.Requisito6 = origen.Requisito6;
        destino.Requisito7 = origen.Requisito7;
        destino.Requisito8 = origen.Requisito8;
        destino.Requisito9 = origen.Requisito9;
        destino.MotivoRechazo = origen.MotivoRechazo;
    }


    private static EvaluacionTramiteViewModel
        CombinarEvaluacion(
            EvaluacionTramiteViewModel actual,
            EvaluacionTramiteViewModel enviada)
    {
        actual.Requisito1 = enviada.Requisito1;
        actual.Requisito2 = enviada.Requisito2;
        actual.Requisito3 = enviada.Requisito3;
        actual.Requisito4 = enviada.Requisito4;
        actual.Requisito5 = enviada.Requisito5;
        actual.Requisito6 = enviada.Requisito6;
        actual.Requisito7 = enviada.Requisito7;
        actual.Requisito8 = enviada.Requisito8;
        actual.Requisito9 = enviada.Requisito9;
        actual.MotivoRechazo = enviada.MotivoRechazo;

        return actual;
    }
}
