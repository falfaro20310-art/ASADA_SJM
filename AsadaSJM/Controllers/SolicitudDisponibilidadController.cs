using AsadaSJM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace AsadaSJM.Controllers;

[Authorize]
public class SolicitudDisponibilidadController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public SolicitudDisponibilidadController(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }


    // =========================================================
    // FORMULARIO
    // =========================================================

    [HttpGet]
    public IActionResult Crear()
    {
        var modelo =
            new SolicitudDisponibilidadViewModel();

        return View(modelo);
    }


    // =========================================================
    // MTG-01 + MTG-02 - REGISTRAR SOLICITUD
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        SolicitudDisponibilidadViewModel modelo)
    {
        // -----------------------------------------------------
        // Validaciones MTG-01
        // -----------------------------------------------------

        if (!modelo.EsPropietarioRegistral &&
            modelo.AutorizacionPropietario == null)
        {
            ModelState.AddModelError(
                nameof(modelo.AutorizacionPropietario),
                "Debe adjuntar un poder especial o autorización autenticada por un abogado.");
        }


        // -----------------------------------------------------
        // MTG-02 - Vigencia certificación literal
        // Máximo 15 días desde su emisión
        // -----------------------------------------------------

        if (modelo.FechaEmisionCertificacionLiteral.HasValue)
        {
            DateTime fechaEmision =
                modelo.FechaEmisionCertificacionLiteral
                    .Value.Date;

            DateTime hoy =
                DateTime.Today;


            if (fechaEmision > hoy)
            {
                ModelState.AddModelError(
                    nameof(modelo.FechaEmisionCertificacionLiteral),
                    "La fecha de emisión no puede ser posterior a la fecha actual.");
            }
            else if (fechaEmision < hoy.AddDays(-15))
            {
                ModelState.AddModelError(
                    nameof(modelo.FechaEmisionCertificacionLiteral),
                    "La certificación literal está vencida. Debe presentar una certificación emitida hace máximo 15 días.");
            }
        }


        // -----------------------------------------------------
        // MTG-02 - Requisitos condicionales
        // -----------------------------------------------------

        if (modelo.EsSegregacionFraccionamiento &&
            modelo.PlanoAgrimensuraFracciones == null &&
            modelo.MinutaRechazo == null)
        {
            ModelState.AddModelError(
                nameof(modelo.PlanoAgrimensuraFracciones),
                "Para una segregación o fraccionamiento debe adjuntar el plano de agrimensura de las fracciones o la minuta de rechazo.");
        }


        // -----------------------------------------------------
        // MTG-02 - Expediente incompleto
        // -----------------------------------------------------

        var documentosPendientes =
            new List<string>();


        if (modelo.Identificacion == null)
        {
            documentosPendientes.Add(
                "Identificación vigente");
        }


        if (!modelo.EsPropietarioRegistral &&
            modelo.AutorizacionPropietario == null)
        {
            documentosPendientes.Add(
                "Poder especial o autorización autenticada");
        }


        if (modelo.CertificacionLiteral == null)
        {
            documentosPendientes.Add(
                "Certificación literal vigente");
        }


        if (modelo.EsSegregacionFraccionamiento &&
            modelo.PlanoAgrimensuraFracciones == null &&
            modelo.MinutaRechazo == null)
        {
            documentosPendientes.Add(
                "Plano de agrimensura de las fracciones o minuta de rechazo");
        }


        if (documentosPendientes.Count > 0)
        {
            ModelState.AddModelError(
                string.Empty,
                "Documentos pendientes: " +
                string.Join(
                    ", ",
                    documentosPendientes) +
                ".");
        }


        if (!ModelState.IsValid)
        {
            return View(modelo);
        }


        // -----------------------------------------------------
        // Usuario autenticado
        // -----------------------------------------------------

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


        if (string.IsNullOrWhiteSpace(connectionString))
        {
            ModelState.AddModelError(
                string.Empty,
                "No fue posible conectar con la base de datos.");

            return View(modelo);
        }


        int? idAbonado =
            await ObtenerIdAbonadoAsync(
                idUsuario.Value);


        using SqlConnection connection =
            new SqlConnection(connectionString);


        await connection.OpenAsync();


        using SqlTransaction transaction =
            connection.BeginTransaction();


        var archivosGuardados =
            new List<string>();


        try
        {
            // =================================================
            // Obtener tipo de trámite
            // =================================================

            const string consultaTipo = @"
                SELECT TOP 1
                    IdTipoTramite
                FROM TIPO_TRAMITE
                WHERE
                    Nombre = @Nombre
                    AND Estado = 1;";


            using SqlCommand comandoTipo =
                new SqlCommand(
                    consultaTipo,
                    connection,
                    transaction);


            comandoTipo.Parameters.AddWithValue(
                "@Nombre",
                "Disponibilidad Hídrica");


            object? resultadoTipo =
                await comandoTipo.ExecuteScalarAsync();


            if (resultadoTipo == null ||
                resultadoTipo == DBNull.Value)
            {
                transaction.Rollback();

                ModelState.AddModelError(
                    string.Empty,
                    "No se encontró el tipo de trámite Disponibilidad Hídrica.");

                return View(modelo);
            }


            int idTipoTramite =
                Convert.ToInt32(
                    resultadoTipo);


            // =================================================
            // Crear TRAMITE
            // =================================================

            const string insertarTramite = @"
                INSERT INTO TRAMITE
                (
                    IdUsuario,
                    IdAbonado,
                    IdTipoTramite,
                    FechaSolicitud,
                    Estado,
                    FechaFinalizacion,
                    Observaciones,
                    IdUsuarioResponsable
                )
                OUTPUT INSERTED.IdTramite
                VALUES
                (
                    @IdUsuario,
                    @IdAbonado,
                    @IdTipoTramite,
                    GETDATE(),
                    'Pendiente',
                    NULL,
                    NULL,
                    NULL
                );";


            using SqlCommand comandoTramite =
                new SqlCommand(
                    insertarTramite,
                    connection,
                    transaction);


            comandoTramite.Parameters.AddWithValue(
                "@IdUsuario",
                idUsuario.Value);


            comandoTramite.Parameters.AddWithValue(
                "@IdAbonado",
                (object?)idAbonado
                ?? DBNull.Value);


            comandoTramite.Parameters.AddWithValue(
                "@IdTipoTramite",
                idTipoTramite);


            int idTramite =
                Convert.ToInt32(
                    await comandoTramite
                        .ExecuteScalarAsync());


            // =================================================
            // Número único
            // =================================================

            string numeroTramite =
                $"DH-{DateTime.Now.Year}-{idTramite:D6}";


            // =================================================
            // Datos específicos de disponibilidad
            // =================================================

            const string insertarSolicitud = @"
                INSERT INTO SOLICITUD_DISPONIBILIDAD_HIDRICA
                (
                    IdTramite,
                    NumeroTramite,
                    NombreSolicitante,
                    PropositoDisponibilidad,
                    DescripcionProyecto,
                    EsPropietarioRegistral,
                    FechaRegistro,
                    EsSegregacionFraccionamiento
                )
                VALUES
                (
                    @IdTramite,
                    @NumeroTramite,
                    @NombreSolicitante,
                    @PropositoDisponibilidad,
                    @DescripcionProyecto,
                    @EsPropietarioRegistral,
                    GETDATE(),
                    @EsSegregacionFraccionamiento
                );";


            using SqlCommand comandoSolicitud =
                new SqlCommand(
                    insertarSolicitud,
                    connection,
                    transaction);


            comandoSolicitud.Parameters.AddWithValue(
                "@IdTramite",
                idTramite);


            comandoSolicitud.Parameters.AddWithValue(
                "@NumeroTramite",
                numeroTramite);


            comandoSolicitud.Parameters.AddWithValue(
                "@NombreSolicitante",
                modelo.NombreSolicitante);


            comandoSolicitud.Parameters.AddWithValue(
                "@PropositoDisponibilidad",
                modelo.PropositoDisponibilidad);


            comandoSolicitud.Parameters.AddWithValue(
                "@DescripcionProyecto",
                modelo.DescripcionProyecto);


            comandoSolicitud.Parameters.AddWithValue(
                "@EsPropietarioRegistral",
                modelo.EsPropietarioRegistral);


            comandoSolicitud.Parameters.AddWithValue(
                "@EsSegregacionFraccionamiento",
                modelo.EsSegregacionFraccionamiento);


            await comandoSolicitud
                .ExecuteNonQueryAsync();


            // =================================================
            // Identificación
            // =================================================

            var identificacionGuardada =
                await GuardarDocumentoAsync(
                    modelo.Identificacion!,
                    idTramite,
                    "identificacion");


            archivosGuardados.Add(
                identificacionGuardada.RutaFisica);


            await RegistrarDocumentoAsync(
                connection,
                transaction,
                idTramite,
                ConstruirNombreDocumento(
                    "Identificación vigente",
                    modelo.Identificacion!.FileName),
                identificacionGuardada.RutaRegistrada,
                "Identificacion",
                null);


            // =================================================
            // Autorización
            // =================================================

            if (!modelo.EsPropietarioRegistral &&
                modelo.AutorizacionPropietario != null)
            {
                var autorizacionGuardada =
                    await GuardarDocumentoAsync(
                        modelo.AutorizacionPropietario,
                        idTramite,
                        "autorizacion");


                archivosGuardados.Add(
                    autorizacionGuardada.RutaFisica);


                await RegistrarDocumentoAsync(
                    connection,
                    transaction,
                    idTramite,
                    ConstruirNombreDocumento(
                        "Autorización autenticada",
                        modelo.AutorizacionPropietario.FileName),
                    autorizacionGuardada.RutaRegistrada,
                    "AutorizacionPropietario",
                    null);
            }


            // =================================================
            // MTG-02 - Certificación literal
            // =================================================

            var certificacionGuardada =
                await GuardarDocumentoAsync(
                    modelo.CertificacionLiteral!,
                    idTramite,
                    "certificacion_literal");


            archivosGuardados.Add(
                certificacionGuardada.RutaFisica);


            await RegistrarDocumentoAsync(
                connection,
                transaction,
                idTramite,
                ConstruirNombreDocumento(
                    "Certificación literal",
                    modelo.CertificacionLiteral!.FileName),
                certificacionGuardada.RutaRegistrada,
                "CertificacionLiteral",
                modelo.FechaEmisionCertificacionLiteral);


            // =================================================
            // MTG-02 - Plano de agrimensura
            // =================================================

            if (modelo.PlanoAgrimensuraFracciones != null)
            {
                var planoGuardado =
                    await GuardarDocumentoAsync(
                        modelo.PlanoAgrimensuraFracciones,
                        idTramite,
                        "plano_agrimensura");


                archivosGuardados.Add(
                    planoGuardado.RutaFisica);


                await RegistrarDocumentoAsync(
                    connection,
                    transaction,
                    idTramite,
                    ConstruirNombreDocumento(
                        "Plano de agrimensura de las fracciones",
                        modelo.PlanoAgrimensuraFracciones.FileName),
                    planoGuardado.RutaRegistrada,
                    "PlanoAgrimensuraFracciones",
                    null);
            }


            // =================================================
            // MTG-02 - Minuta de rechazo
            // =================================================

            if (modelo.MinutaRechazo != null)
            {
                var minutaGuardada =
                    await GuardarDocumentoAsync(
                        modelo.MinutaRechazo,
                        idTramite,
                        "minuta_rechazo");


                archivosGuardados.Add(
                    minutaGuardada.RutaFisica);


                await RegistrarDocumentoAsync(
                    connection,
                    transaction,
                    idTramite,
                    ConstruirNombreDocumento(
                        "Minuta de rechazo",
                        modelo.MinutaRechazo.FileName),
                    minutaGuardada.RutaRegistrada,
                    "MinutaRechazo",
                    null);
            }


            // =================================================
            // Confirmar
            // =================================================

            transaction.Commit();


            TempData["NumeroTramite"] =
                numeroTramite;


            return RedirectToAction(
                nameof(Crear));
        }
        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch
            {
                // La transacción ya pudo estar cerrada.
            }


            foreach (string ruta in archivosGuardados)
            {
                if (System.IO.File.Exists(ruta))
                {
                    System.IO.File.Delete(ruta);
                }
            }


            ModelState.AddModelError(
                string.Empty,
                "Ocurrió un error al registrar la solicitud. Intente nuevamente.");


            return View(modelo);
        }
    }


    // =========================================================
    // USUARIO ACTUAL
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


        if (!int.TryParse(
            valorIdUsuario,
            out int idUsuario))
        {
            return null;
        }


        return idUsuario;
    }


    // =========================================================
    // ABONADO DEL USUARIO
    // =========================================================

    private async Task<int?> ObtenerIdAbonadoAsync(
        int idUsuario)
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


        const string consulta = @"
            SELECT TOP 1
                IdAbonado
            FROM ABONADO
            WHERE
                IdUsuario = @IdUsuario
                AND Estado = 1
            ORDER BY IdAbonado;";


        using SqlCommand comando =
            new SqlCommand(
                consulta,
                connection);


        comando.Parameters.AddWithValue(
            "@IdUsuario",
            idUsuario);


        await connection.OpenAsync();


        object? resultado =
            await comando
                .ExecuteScalarAsync();


        if (resultado == null ||
            resultado == DBNull.Value)
        {
            return null;
        }


        return Convert.ToInt32(
            resultado);
    }


    // =========================================================
    // GUARDAR ARCHIVO PRIVADO
    // =========================================================

    private async Task<(
        string RutaRegistrada,
        string RutaFisica)>
        GuardarDocumentoAsync(
            IFormFile archivo,
            int idTramite,
            string prefijo)
    {
        string carpeta =
            Path.Combine(
                _environment.ContentRootPath,
                "App_Data",
                "tramites",
                idTramite.ToString());


        Directory.CreateDirectory(
            carpeta);


        string extension =
            Path.GetExtension(
                archivo.FileName)
            .ToLowerInvariant();


        string nombre =
            $"{prefijo}_{Guid.NewGuid():N}{extension}";


        string rutaFisica =
            Path.Combine(
                carpeta,
                nombre);


        await using var destino =
            new FileStream(
                rutaFisica,
                FileMode.CreateNew);


        await archivo.CopyToAsync(
            destino);


        string rutaRegistrada =
            $"App_Data/tramites/{idTramite}/{nombre}";


        return (
            rutaRegistrada,
            rutaFisica);
    }


    // =========================================================
    // REGISTRAR DOCUMENTO
    // =========================================================

    private static async Task RegistrarDocumentoAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int idTramite,
        string nombreArchivo,
        string rutaArchivo,
        string tipoDocumento,
        DateTime? fechaEmision)
    {
        const string insertarDocumento = @"
            INSERT INTO TRAMITE_DOCUMENTO
            (
                IdTramite,
                NombreArchivo,
                RutaArchivo,
                FechaCarga,
                TipoDocumento,
                FechaEmision
            )
            VALUES
            (
                @IdTramite,
                @NombreArchivo,
                @RutaArchivo,
                GETDATE(),
                @TipoDocumento,
                @FechaEmision
            );";


        using SqlCommand comando =
            new SqlCommand(
                insertarDocumento,
                connection,
                transaction);


        comando.Parameters.AddWithValue(
            "@IdTramite",
            idTramite);


        comando.Parameters.AddWithValue(
            "@NombreArchivo",
            nombreArchivo);


        comando.Parameters.AddWithValue(
            "@RutaArchivo",
            rutaArchivo);


        comando.Parameters.AddWithValue(
            "@TipoDocumento",
            tipoDocumento);


        comando.Parameters.AddWithValue(
            "@FechaEmision",
            (object?)fechaEmision?.Date
            ?? DBNull.Value);


        await comando
            .ExecuteNonQueryAsync();
    }


    // =========================================================
    // NOMBRE LEGIBLE
    // =========================================================

    private static string ConstruirNombreDocumento(
        string tipoDocumento,
        string nombreOriginal)
    {
        string nombreSeguro =
            Path.GetFileName(
                nombreOriginal);


        string nombre =
            $"{tipoDocumento} - {nombreSeguro}";


        if (nombre.Length > 200)
        {
            nombre =
                nombre[..200];
        }


        return nombre;
    }
}