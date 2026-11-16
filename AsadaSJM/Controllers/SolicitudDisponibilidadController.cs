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
    // MTG-01 - FORMULARIO DE SOLICITUD
    // =========================================================

    [HttpGet]
    public IActionResult Crear()
    {
        var modelo = new SolicitudDisponibilidadViewModel();

        return View(modelo);
    }


    // =========================================================
    // MTG-01 - REGISTRAR SOLICITUD
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        SolicitudDisponibilidadViewModel modelo)
    {
        // Si no es propietario registral,
        // la autorización es obligatoria.
        if (!modelo.EsPropietarioRegistral &&
            modelo.AutorizacionPropietario == null)
        {
            ModelState.AddModelError(
                nameof(modelo.AutorizacionPropietario),
                "Debe adjuntar un poder especial o autorización autenticada por un abogado.");
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }


        // -----------------------------------------------------
        // Obtener usuario autenticado
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


        // Un usuario puede no tener un abonado asociado.
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
            // -------------------------------------------------
            // Obtener tipo de trámite
            // -------------------------------------------------

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
                Convert.ToInt32(resultadoTipo);


            // -------------------------------------------------
            // Crear trámite general
            // -------------------------------------------------

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
                (object?)idAbonado ?? DBNull.Value);


            comandoTramite.Parameters.AddWithValue(
                "@IdTipoTramite",
                idTipoTramite);


            int idTramite =
                Convert.ToInt32(
                    await comandoTramite.ExecuteScalarAsync());


            // -------------------------------------------------
            // Generar número único
            // Ejemplo: DH-2026-000001
            // -------------------------------------------------

            string numeroTramite =
                $"DH-{DateTime.Now.Year}-{idTramite:D6}";


            // -------------------------------------------------
            // Registrar datos específicos de disponibilidad
            // -------------------------------------------------

            const string insertarSolicitud = @"
                INSERT INTO SOLICITUD_DISPONIBILIDAD_HIDRICA
                (
                    IdTramite,
                    NumeroTramite,
                    NombreSolicitante,
                    PropositoDisponibilidad,
                    DescripcionProyecto,
                    EsPropietarioRegistral,
                    FechaRegistro
                )
                VALUES
                (
                    @IdTramite,
                    @NumeroTramite,
                    @NombreSolicitante,
                    @PropositoDisponibilidad,
                    @DescripcionProyecto,
                    @EsPropietarioRegistral,
                    GETDATE()
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


            await comandoSolicitud.ExecuteNonQueryAsync();


            // -------------------------------------------------
            // Guardar identificación
            // -------------------------------------------------

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
                identificacionGuardada.RutaPublica);


            // -------------------------------------------------
            // Guardar autorización cuando corresponda
            // -------------------------------------------------

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
                    autorizacionGuardada.RutaPublica);
            }


            // -------------------------------------------------
            // Confirmar todo
            // -------------------------------------------------

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
                // La transacción ya pudo haberse cerrado.
            }


            // Eliminar archivos físicos si falló la BD.
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
    // OBTENER ID DEL USUARIO AUTENTICADO
    // =========================================================

    private int? ObtenerIdUsuarioActual()
    {
        string? valorIdUsuario =
            User.FindFirst("IdUsuario")?.Value;

        if (string.IsNullOrWhiteSpace(valorIdUsuario))
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
    // OBTENER ABONADO ASOCIADO AL USUARIO
    // =========================================================

    private async Task<int?> ObtenerIdAbonadoAsync(
        int idUsuario)
    {
        string? connectionString =
            _configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        using SqlConnection connection =
            new SqlConnection(connectionString);

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
            await comando.ExecuteScalarAsync();


        if (resultado == null ||
            resultado == DBNull.Value)
        {
            return null;
        }


        return Convert.ToInt32(resultado);
    }


    // =========================================================
    // GUARDAR DOCUMENTO FÍSICO
    // FUERA DE WWWROOT POR SEGURIDAD
    // =========================================================

    private async Task<(string RutaPublica, string RutaFisica)>
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


        Directory.CreateDirectory(carpeta);


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


        await archivo.CopyToAsync(destino);


        string rutaRegistrada =
            $"App_Data/tramites/{idTramite}/{nombre}";


        return (
            rutaRegistrada,
            rutaFisica);
    }


    // =========================================================
    // REGISTRAR DOCUMENTO EN BASE DE DATOS
    // =========================================================

    private static async Task RegistrarDocumentoAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int idTramite,
        string nombreArchivo,
        string rutaArchivo)
    {
        const string insertarDocumento = @"
            INSERT INTO TRAMITE_DOCUMENTO
            (
                IdTramite,
                NombreArchivo,
                RutaArchivo,
                FechaCarga
            )
            VALUES
            (
                @IdTramite,
                @NombreArchivo,
                @RutaArchivo,
                GETDATE()
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


        await comando.ExecuteNonQueryAsync();
    }


    // =========================================================
    // NOMBRE LEGIBLE DEL DOCUMENTO
    // Máximo permitido por la BD: 200 caracteres
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