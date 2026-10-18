using AsadaSJM.Models;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Net;
using System.Text;
using MimeKit;

namespace AsadaSJM.Controllers;

public class TransparenciaController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IDataProtector _protector;

    private const long TamanoMaximoArchivo = 10 * 1024 * 1024;


    public TransparenciaController(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IDataProtectionProvider dataProtectionProvider)
    {
        _configuration = configuration;
        _environment = environment;

        _protector =
            dataProtectionProvider.CreateProtector(
                "AsadaSJM.ConfiguracionCorreo.Password");
    }


    // =========================================================
    // LISTADO DE DOCUMENTOS
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Index(
        int? idCategoria,
        int? anio,
        string? periodo)
    {
        var documentos =
            new List<DocumentoTransparencia>();

        var categorias =
            new List<CategoriaDocumento>();

        var anios =
            new List<int>();


        // =====================================================
        // NORMALIZAR FILTROS
        // =====================================================

        if (idCategoria <= 0)
        {
            idCategoria = null;
        }

        if (anio <= 0)
        {
            anio = null;
        }

        periodo =
            string.IsNullOrWhiteSpace(periodo)
                ? null
                : periodo.Trim();


        ViewBag.IdCategoriaSeleccionada =
            idCategoria;

        ViewBag.AnioSeleccionado =
            anio;

        ViewBag.PeriodoSeleccionado =
            periodo ?? string.Empty;

        ViewBag.FiltrosAplicados =
            idCategoria.HasValue ||
            anio.HasValue ||
            !string.IsNullOrWhiteSpace(periodo);


        // =====================================================
        // CONEXIÓN
        // =====================================================

        string? connectionString =
            _configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            ViewBag.Error =
                "No se encontró la conexión a la base de datos.";

            ViewBag.Categorias =
                categorias;

            ViewBag.Anios =
                anios;

            return View(documentos);
        }


        await using SqlConnection connection =
            new SqlConnection(
                connectionString);

        await connection.OpenAsync();


        // =====================================================
        // CARGAR CATEGORÍAS PARA EL FILTRO
        // =====================================================

        const string consultaCategorias = @"
            SELECT
                IdCategoria,
                Nombre,
                Descripcion,
                Estado
            FROM CATEGORIA_DOCUMENTO
            WHERE Estado = 1
            ORDER BY Nombre;";


        await using (
            SqlCommand commandCategorias =
                new SqlCommand(
                    consultaCategorias,
                    connection))
        {
            await using SqlDataReader readerCategorias =
                await commandCategorias.ExecuteReaderAsync();

            while (await readerCategorias.ReadAsync())
            {
                categorias.Add(
                    new CategoriaDocumento
                    {
                        IdCategoria =
                            Convert.ToInt32(
                                readerCategorias["IdCategoria"]),

                        Nombre =
                            readerCategorias["Nombre"]?.ToString()
                            ?? string.Empty,

                        Descripcion =
                            readerCategorias["Descripcion"] == DBNull.Value
                                ? null
                                : readerCategorias["Descripcion"]?.ToString(),

                        Estado =
                            Convert.ToBoolean(
                                readerCategorias["Estado"])
                    });
            }
        }


        // =====================================================
        // CARGAR AÑOS DISPONIBLES
        // =====================================================

        const string consultaAnios = @"
            SELECT DISTINCT
                YEAR(D.FechaPublicacion) AS Anio
            FROM DOCUMENTO_TRANSPARENCIA D
            INNER JOIN CATEGORIA_DOCUMENTO C
                ON C.IdCategoria = D.IdCategoria
            WHERE
                D.Estado = 1
                AND C.Estado = 1
            ORDER BY Anio DESC;";


        await using (
            SqlCommand commandAnios =
                new SqlCommand(
                    consultaAnios,
                    connection))
        {
            await using SqlDataReader readerAnios =
                await commandAnios.ExecuteReaderAsync();

            while (await readerAnios.ReadAsync())
            {
                anios.Add(
                    Convert.ToInt32(
                        readerAnios["Anio"]));
            }
        }


        ViewBag.Categorias =
            categorias;

        ViewBag.Anios =
            anios;


        // =====================================================
        // CONSULTA DE DOCUMENTOS CON FILTROS
        // =====================================================

        StringBuilder consulta =
            new StringBuilder(@"
                SELECT
                    D.IdDocumento,
                    D.IdCategoria,
                    D.Titulo,
                    D.Descripcion,
                    D.Periodo,
                    D.RutaArchivo,
                    D.FechaPublicacion,
                    D.IdUsuarioPublicador,
                    D.Estado,
                    C.Nombre AS NombreCategoria
                FROM DOCUMENTO_TRANSPARENCIA D
                INNER JOIN CATEGORIA_DOCUMENTO C
                    ON C.IdCategoria = D.IdCategoria
                WHERE
                    D.Estado = 1
                    AND C.Estado = 1");


        await using SqlCommand command =
            new SqlCommand();

        command.Connection =
            connection;


        // -----------------------------------------------------
        // FILTRO POR CATEGORÍA
        // -----------------------------------------------------

        if (idCategoria.HasValue)
        {
            consulta.Append(@"
                AND D.IdCategoria = @IdCategoria");

            command.Parameters.Add(
                "@IdCategoria",
                SqlDbType.Int).Value =
                idCategoria.Value;
        }


        // -----------------------------------------------------
        // FILTRO POR AÑO
        // -----------------------------------------------------

        if (anio.HasValue)
        {
            consulta.Append(@"
                AND YEAR(D.FechaPublicacion) = @Anio");

            command.Parameters.Add(
                "@Anio",
                SqlDbType.Int).Value =
                anio.Value;
        }


        // -----------------------------------------------------
        // FILTRO POR PERÍODO
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(periodo))
        {
            consulta.Append(@"
                AND D.Periodo LIKE @Periodo");

            command.Parameters.Add(
                "@Periodo",
                SqlDbType.VarChar,
                50).Value =
                $"%{periodo}%";
        }


        consulta.Append(@"
            ORDER BY
                D.FechaPublicacion DESC,
                D.IdDocumento DESC;");


        command.CommandText =
            consulta.ToString();


        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();


        while (await reader.ReadAsync())
        {
            documentos.Add(
                new DocumentoTransparencia
                {
                    IdDocumento =
                        Convert.ToInt32(
                            reader["IdDocumento"]),

                    IdCategoria =
                        Convert.ToInt32(
                            reader["IdCategoria"]),

                    Titulo =
                        reader["Titulo"]?.ToString()
                        ?? string.Empty,

                    Descripcion =
                        reader["Descripcion"] == DBNull.Value
                            ? null
                            : reader["Descripcion"]?.ToString(),

                    Periodo =
                        reader["Periodo"] == DBNull.Value
                            ? null
                            : reader["Periodo"]?.ToString(),

                    RutaArchivo =
                        reader["RutaArchivo"]?.ToString()
                        ?? string.Empty,

                    FechaPublicacion =
                        Convert.ToDateTime(
                            reader["FechaPublicacion"]),

                    IdUsuarioPublicador =
                        Convert.ToInt32(
                            reader["IdUsuarioPublicador"]),

                    Estado =
                        Convert.ToBoolean(
                            reader["Estado"]),

                    NombreCategoria =
                        reader["NombreCategoria"]?.ToString()
                        ?? string.Empty
                });
        }


        return View(documentos);
    }


    // =========================================================
    // REGISTRAR DOCUMENTO - GET
    // =========================================================

    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public async Task<IActionResult> Registrar()
    {
        var modelo =
            new DocumentoTransparenciaViewModel();

        await CargarCategoriasAsync(
            modelo);

        return View(
            modelo);
    }


    // =========================================================
    // REGISTRAR DOCUMENTO - POST
    // =========================================================

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(
        DocumentoTransparenciaViewModel modelo)
    {
        string? connectionString =
            _configuration.GetConnectionString(
                "DefaultConnection");


        // -----------------------------------------------------
        // 1. Validar conexión
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(
                connectionString))
        {
            ModelState.AddModelError(
                string.Empty,
                "No se encontró la conexión a la base de datos.");
        }


        // -----------------------------------------------------
        // 2. Validar usuario autenticado
        // -----------------------------------------------------

        string? idUsuarioClaim =
            User.FindFirst(
                "IdUsuario")?.Value;

        if (!int.TryParse(
                idUsuarioClaim,
                out int idUsuarioPublicador))
        {
            ModelState.AddModelError(
                string.Empty,
                "No fue posible identificar al usuario autenticado. Cierre sesión e ingrese nuevamente.");
        }


        // -----------------------------------------------------
        // 3. Validar archivo
        // -----------------------------------------------------

        if (modelo.ArchivoPDF == null ||
            modelo.ArchivoPDF.Length == 0)
        {
            ModelState.AddModelError(
                nameof(modelo.ArchivoPDF),
                "Debe seleccionar un archivo PDF.");
        }
        else
        {
            string extension =
                Path.GetExtension(
                    modelo.ArchivoPDF.FileName);

            if (!extension.Equals(
                    ".pdf",
                    StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    nameof(modelo.ArchivoPDF),
                    "Únicamente se permiten archivos PDF.");
            }


            if (modelo.ArchivoPDF.Length >
                TamanoMaximoArchivo)
            {
                ModelState.AddModelError(
                    nameof(modelo.ArchivoPDF),
                    "El archivo no puede superar los 10 MB.");
            }


            if (!await EsArchivoPdfValidoAsync(
                    modelo.ArchivoPDF))
            {
                ModelState.AddModelError(
                    nameof(modelo.ArchivoPDF),
                    "El archivo seleccionado no corresponde a un PDF válido.");
            }
        }


        // -----------------------------------------------------
        // 4. Validar categoría
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
                connectionString) &&
            modelo.IdCategoria > 0)
        {
            bool categoriaValida =
                await CategoriaExisteYEstaActivaAsync(
                    modelo.IdCategoria,
                    connectionString);

            if (!categoriaValida)
            {
                ModelState.AddModelError(
                    nameof(modelo.IdCategoria),
                    "La categoría seleccionada no es válida o se encuentra inactiva.");
            }
        }


        // -----------------------------------------------------
        // 5. Si existen errores, volver al formulario
        // -----------------------------------------------------

        if (!ModelState.IsValid)
        {
            await CargarCategoriasAsync(
                modelo);

            return View(
                modelo);
        }


        // -----------------------------------------------------
        // 6. Preparar archivo
        // -----------------------------------------------------

        string nombreArchivo =
            $"{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}.pdf";

        string carpetaRelativa =
            Path.Combine(
                "documentos",
                "transparencia");

        string webRoot =
            _environment.WebRootPath
            ?? Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot");

        string carpetaFisica =
            Path.Combine(
                webRoot,
                carpetaRelativa);

        Directory.CreateDirectory(
            carpetaFisica);

        string rutaFisica =
            Path.Combine(
                carpetaFisica,
                nombreArchivo);

        string rutaArchivo =
            $"/documentos/transparencia/{nombreArchivo}";


        try
        {
            // -------------------------------------------------
            // 7. Guardar PDF
            // -------------------------------------------------

            await using (
                FileStream stream =
                    new FileStream(
                        rutaFisica,
                        FileMode.Create))
            {
                await modelo.ArchivoPDF!
                    .CopyToAsync(
                        stream);
            }


            // -------------------------------------------------
            // 8. Registrar documento en SQL Server
            // -------------------------------------------------

            const string consulta = @"
                INSERT INTO DOCUMENTO_TRANSPARENCIA
                (
                    IdCategoria,
                    Titulo,
                    Descripcion,
                    Periodo,
                    RutaArchivo,
                    FechaPublicacion,
                    IdUsuarioPublicador,
                    Estado
                )
                VALUES
                (
                    @IdCategoria,
                    @Titulo,
                    @Descripcion,
                    @Periodo,
                    @RutaArchivo,
                    @FechaPublicacion,
                    @IdUsuarioPublicador,
                    1
                );";


            await using SqlConnection connection =
                new SqlConnection(
                    connectionString);

            await using SqlCommand command =
                new SqlCommand(
                    consulta,
                    connection);


            command.Parameters.Add(
                "@IdCategoria",
                SqlDbType.Int).Value =
                modelo.IdCategoria;


            command.Parameters.Add(
                "@Titulo",
                SqlDbType.VarChar,
                200).Value =
                modelo.Titulo.Trim();


            command.Parameters.Add(
                "@Descripcion",
                SqlDbType.VarChar,
                500).Value =
                string.IsNullOrWhiteSpace(
                    modelo.Descripcion)
                    ? DBNull.Value
                    : modelo.Descripcion.Trim();


            command.Parameters.Add(
                "@Periodo",
                SqlDbType.VarChar,
                50).Value =
                string.IsNullOrWhiteSpace(
                    modelo.Periodo)
                    ? DBNull.Value
                    : modelo.Periodo.Trim();


            command.Parameters.Add(
                "@RutaArchivo",
                SqlDbType.VarChar,
                500).Value =
                rutaArchivo;


            command.Parameters.Add(
                "@FechaPublicacion",
                SqlDbType.DateTime).Value =
                modelo.FechaPublicacion;


            command.Parameters.Add(
                "@IdUsuarioPublicador",
                SqlDbType.Int).Value =
                idUsuarioPublicador;


            await connection.OpenAsync();

            await command.ExecuteNonQueryAsync();


            // -------------------------------------------------
            // 9. Enviar notificaciones a suscriptores
            // -------------------------------------------------

            var resultadoNotificaciones =
                await EnviarNotificacionesDocumentoAsync(
                    modelo.IdCategoria,
                    modelo.Titulo.Trim(),
                    modelo.Descripcion,
                    modelo.Periodo,
                    modelo.FechaPublicacion,
                    rutaArchivo,
                    connectionString!);


            // -------------------------------------------------
            // 10. Registro exitoso
            // -------------------------------------------------

            if (resultadoNotificaciones.Enviados > 0 &&
                resultadoNotificaciones.Fallidos == 0)
            {
                TempData["MensajeExito"] =
                    $"El documento fue publicado correctamente y se enviaron {resultadoNotificaciones.Enviados} notificación(es).";
            }
            else if (resultadoNotificaciones.Enviados > 0 &&
                     resultadoNotificaciones.Fallidos > 0)
            {
                TempData["MensajeExito"] =
                    $"El documento fue publicado correctamente. Se enviaron {resultadoNotificaciones.Enviados} notificación(es), pero {resultadoNotificaciones.Fallidos} no pudieron enviarse.";
            }
            else if (resultadoNotificaciones.Fallidos > 0)
            {
                TempData["MensajeExito"] =
                    "El documento fue publicado correctamente, pero no fue posible enviar las notificaciones por correo.";
            }
            else
            {
                TempData["MensajeExito"] =
                    "El documento fue publicado correctamente.";
            }

            return RedirectToAction(
                nameof(Index));
        }
        catch
        {
            // Si falló el registro en BD,
            // eliminar el archivo físico para no dejar basura.

            if (System.IO.File.Exists(
                    rutaFisica))
            {
                System.IO.File.Delete(
                    rutaFisica);
            }


            ModelState.AddModelError(
                string.Empty,
                "Ocurrió un error al registrar el documento. Intente nuevamente.");

            await CargarCategoriasAsync(
                modelo);

            return View(
                modelo);
        }
    }


    // =========================================================
    // SUSCRIPCIONES - GET
    // =========================================================

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Suscripciones()
    {
        var modelo =
            new SuscripcionDocumentoViewModel();

        string? connectionString =
            _configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrWhiteSpace(
                connectionString))
        {
            ViewBag.Error =
                "No se encontró la conexión a la base de datos.";

            return View(
                modelo);
        }

        string? idUsuarioClaim =
            User.FindFirst(
                "IdUsuario")?.Value;

        if (!int.TryParse(
                idUsuarioClaim,
                out int idUsuario))
        {
            ViewBag.Error =
                "No fue posible identificar al usuario autenticado. Cierre sesión e ingrese nuevamente.";

            return View(
                modelo);
        }

        await CargarDatosSuscripcionAsync(
            modelo,
            idUsuario,
            connectionString,
            cargarSeleccionadasDesdeBaseDatos: true);

        return View(
            modelo);
    }


    // =========================================================
    // SUSCRIPCIONES - POST
    // =========================================================

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suscripciones(
        SuscripcionDocumentoViewModel modelo)
    {
        string? connectionString =
            _configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrWhiteSpace(
                connectionString))
        {
            ModelState.AddModelError(
                string.Empty,
                "No se encontró la conexión a la base de datos.");

            return View(
                modelo);
        }

        string? idUsuarioClaim =
            User.FindFirst(
                "IdUsuario")?.Value;

        if (!int.TryParse(
                idUsuarioClaim,
                out int idUsuario))
        {
            ModelState.AddModelError(
                string.Empty,
                "No fue posible identificar al usuario autenticado. Cierre sesión e ingrese nuevamente.");

            await CargarDatosSuscripcionAsync(
                modelo,
                0,
                connectionString,
                cargarSeleccionadasDesdeBaseDatos: false);

            return View(
                modelo);
        }

        modelo.CategoriasSeleccionadas =
            modelo.CategoriasSeleccionadas
                .Distinct()
                .ToList();

        var categoriasActivas =
            new HashSet<int>();

        const string consultaCategoriasActivas = @"
            SELECT IdCategoria
            FROM CATEGORIA_DOCUMENTO
            WHERE Estado = 1;";

        await using (
            SqlConnection connectionCategorias =
                new SqlConnection(
                    connectionString))
        {
            await connectionCategorias.OpenAsync();

            await using SqlCommand commandCategorias =
                new SqlCommand(
                    consultaCategoriasActivas,
                    connectionCategorias);

            await using SqlDataReader readerCategorias =
                await commandCategorias.ExecuteReaderAsync();

            while (await readerCategorias.ReadAsync())
            {
                categoriasActivas.Add(
                    Convert.ToInt32(
                        readerCategorias["IdCategoria"]));
            }
        }

        bool existeCategoriaInvalida =
            modelo.CategoriasSeleccionadas.Any(
                idCategoria =>
                    !categoriasActivas.Contains(
                        idCategoria));

        if (existeCategoriaInvalida)
        {
            ModelState.AddModelError(
                nameof(modelo.CategoriasSeleccionadas),
                "Una o más categorías seleccionadas no son válidas o se encuentran inactivas.");
        }

        if (!ModelState.IsValid)
        {
            await CargarDatosSuscripcionAsync(
                modelo,
                idUsuario,
                connectionString,
                cargarSeleccionadasDesdeBaseDatos: false);

            return View(
                modelo);
        }

        await using SqlConnection connection =
            new SqlConnection(
                connectionString);

        await connection.OpenAsync();

        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            var categoriasActualmenteActivas =
                new HashSet<int>();

            const string consultaSuscripcionesActuales = @"
                SELECT IdCategoria
                FROM SUSCRIPCION_DOCUMENTO
                WHERE
                    IdUsuario = @IdUsuario
                    AND Estado = 1;";

            await using (
                SqlCommand commandActuales =
                    new SqlCommand(
                        consultaSuscripcionesActuales,
                        connection,
                        transaction))
            {
                commandActuales.Parameters.Add(
                    "@IdUsuario",
                    SqlDbType.Int).Value =
                    idUsuario;

                await using SqlDataReader readerActuales =
                    await commandActuales.ExecuteReaderAsync();

                while (await readerActuales.ReadAsync())
                {
                    categoriasActualmenteActivas.Add(
                        Convert.ToInt32(
                            readerActuales["IdCategoria"]));
                }
            }

            var categoriasADesactivar =
                categoriasActualmenteActivas
                    .Except(
                        modelo.CategoriasSeleccionadas)
                    .ToList();

            const string desactivarSuscripcion = @"
                UPDATE SUSCRIPCION_DOCUMENTO
                SET Estado = 0
                WHERE
                    IdUsuario = @IdUsuario
                    AND IdCategoria = @IdCategoria;";

            foreach (int idCategoria in categoriasADesactivar)
            {
                await using SqlCommand commandDesactivar =
                    new SqlCommand(
                        desactivarSuscripcion,
                        connection,
                        transaction);

                commandDesactivar.Parameters.Add(
                    "@IdUsuario",
                    SqlDbType.Int).Value =
                    idUsuario;

                commandDesactivar.Parameters.Add(
                    "@IdCategoria",
                    SqlDbType.Int).Value =
                    idCategoria;

                await commandDesactivar.ExecuteNonQueryAsync();
            }

            const string guardarSuscripcion = @"
                MERGE SUSCRIPCION_DOCUMENTO AS destino
                USING
                (
                    SELECT
                        @IdUsuario AS IdUsuario,
                        @IdCategoria AS IdCategoria
                ) AS origen
                ON
                    destino.IdUsuario = origen.IdUsuario
                    AND destino.IdCategoria = origen.IdCategoria

                WHEN MATCHED AND destino.Estado = 0 THEN
                    UPDATE SET
                        Estado = 1,
                        FechaSuscripcion = GETDATE()

                WHEN NOT MATCHED THEN
                    INSERT
                    (
                        IdUsuario,
                        IdCategoria,
                        FechaSuscripcion,
                        Estado
                    )
                    VALUES
                    (
                        @IdUsuario,
                        @IdCategoria,
                        GETDATE(),
                        1
                    );";

            foreach (int idCategoria in modelo.CategoriasSeleccionadas)
            {
                await using SqlCommand commandGuardar =
                    new SqlCommand(
                        guardarSuscripcion,
                        connection,
                        transaction);

                commandGuardar.Parameters.Add(
                    "@IdUsuario",
                    SqlDbType.Int).Value =
                    idUsuario;

                commandGuardar.Parameters.Add(
                    "@IdCategoria",
                    SqlDbType.Int).Value =
                    idCategoria;

                await commandGuardar.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();

            TempData["MensajeSuscripcion"] =
                modelo.CategoriasSeleccionadas.Count > 0
                    ? "Sus preferencias de notificación fueron guardadas correctamente."
                    : "Se desactivaron todas sus suscripciones a documentos.";

            return RedirectToAction(
                nameof(Suscripciones));
        }
        catch
        {
            await transaction.RollbackAsync();

            ModelState.AddModelError(
                string.Empty,
                "Ocurrió un error al guardar las preferencias de notificación. Intente nuevamente.");

            await CargarDatosSuscripcionAsync(
                modelo,
                idUsuario,
                connectionString,
                cargarSeleccionadasDesdeBaseDatos: false);

            return View(
                modelo);
        }
    }


    // =========================================================
    // CARGAR DATOS DE SUSCRIPCIÓN
    // =========================================================

    private static async Task CargarDatosSuscripcionAsync(
        SuscripcionDocumentoViewModel modelo,
        int idUsuario,
        string connectionString,
        bool cargarSeleccionadasDesdeBaseDatos)
    {
        modelo.Categorias.Clear();

        if (cargarSeleccionadasDesdeBaseDatos)
        {
            modelo.CategoriasSeleccionadas.Clear();
        }

        const string consulta = @"
            SELECT
                C.IdCategoria,
                C.Nombre,
                C.Descripcion,
                C.Estado,
                CASE
                    WHEN S.IdSuscripcion IS NOT NULL
                         AND S.Estado = 1
                    THEN 1
                    ELSE 0
                END AS Suscrito
            FROM CATEGORIA_DOCUMENTO C
            LEFT JOIN SUSCRIPCION_DOCUMENTO S
                ON S.IdCategoria = C.IdCategoria
                AND S.IdUsuario = @IdUsuario
            WHERE C.Estado = 1
            ORDER BY C.Nombre;";

        await using SqlConnection connection =
            new SqlConnection(
                connectionString);

        await using SqlCommand command =
            new SqlCommand(
                consulta,
                connection);

        command.Parameters.Add(
            "@IdUsuario",
            SqlDbType.Int).Value =
            idUsuario;

        await connection.OpenAsync();

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            int idCategoria =
                Convert.ToInt32(
                    reader["IdCategoria"]);

            modelo.Categorias.Add(
                new CategoriaDocumento
                {
                    IdCategoria =
                        idCategoria,

                    Nombre =
                        reader["Nombre"]?.ToString()
                        ?? string.Empty,

                    Descripcion =
                        reader["Descripcion"] == DBNull.Value
                            ? null
                            : reader["Descripcion"]?.ToString(),

                    Estado =
                        Convert.ToBoolean(
                            reader["Estado"])
                });

            if (cargarSeleccionadasDesdeBaseDatos &&
                Convert.ToBoolean(
                    reader["Suscrito"]))
            {
                modelo.CategoriasSeleccionadas.Add(
                    idCategoria);
            }
        }
    }


    // =========================================================
    // ENVIAR NOTIFICACIONES DE NUEVO DOCUMENTO
    // =========================================================

    private async Task<(int Enviados, int Fallidos)>
        EnviarNotificacionesDocumentoAsync(
            int idCategoria,
            string titulo,
            string? descripcion,
            string? periodo,
            DateTime fechaPublicacion,
            string rutaArchivo,
            string connectionString)
    {
        var destinatarios =
            new List<string>();

        string nombreCategoria =
            string.Empty;


        // -----------------------------------------------------
        // 1. Obtener usuarios suscritos a la categoría
        // -----------------------------------------------------

        const string consultaDestinatarios = @"
            SELECT DISTINCT
                U.CorreoElectronico,
                C.Nombre AS NombreCategoria
            FROM SUSCRIPCION_DOCUMENTO S
            INNER JOIN USUARIO U
                ON U.IdUsuario = S.IdUsuario
            INNER JOIN CATEGORIA_DOCUMENTO C
                ON C.IdCategoria = S.IdCategoria
            WHERE
                S.IdCategoria = @IdCategoria
                AND S.Estado = 1
                AND U.Estado = 1
                AND C.Estado = 1
                AND U.CorreoElectronico IS NOT NULL
                AND LTRIM(RTRIM(U.CorreoElectronico)) <> '';";


        await using (
            SqlConnection connection =
                new SqlConnection(
                    connectionString))
        {
            await connection.OpenAsync();

            await using SqlCommand command =
                new SqlCommand(
                    consultaDestinatarios,
                    connection);

            command.Parameters.Add(
                "@IdCategoria",
                SqlDbType.Int).Value =
                idCategoria;


            await using SqlDataReader reader =
                await command.ExecuteReaderAsync();


            while (await reader.ReadAsync())
            {
                string correo =
                    reader["CorreoElectronico"]?.ToString()?.Trim()
                    ?? string.Empty;


                if (!string.IsNullOrWhiteSpace(
                        correo))
                {
                    destinatarios.Add(
                        correo);
                }


                if (string.IsNullOrWhiteSpace(
                        nombreCategoria))
                {
                    nombreCategoria =
                        reader["NombreCategoria"]?.ToString()
                        ?? string.Empty;
                }
            }
        }


        if (destinatarios.Count == 0)
        {
            return (
                Enviados: 0,
                Fallidos: 0);
        }


        // -----------------------------------------------------
        // 2. Obtener configuración SMTP
        // -----------------------------------------------------

        string host =
            string.Empty;

        int port =
            587;

        string username =
            string.Empty;

        string passwordEncriptado =
            string.Empty;

        string fromEmail =
            string.Empty;

        string fromName =
            string.Empty;


        try
        {
            const string consultaCorreo = @"
                SELECT
                    Host,
                    Port,
                    Username,
                    PasswordEncriptado,
                    FromEmail,
                    FromName
                FROM ConfiguracionCorreo
                WHERE Id = 1;";


            await using SqlConnection connectionCorreo =
                new SqlConnection(
                    connectionString);

            await using SqlCommand commandCorreo =
                new SqlCommand(
                    consultaCorreo,
                    connectionCorreo);

            await connectionCorreo.OpenAsync();


            await using SqlDataReader readerCorreo =
                await commandCorreo.ExecuteReaderAsync();


            if (!await readerCorreo.ReadAsync())
            {
                return (
                    Enviados: 0,
                    Fallidos: destinatarios.Count);
            }


            host =
                readerCorreo["Host"]?.ToString()?.Trim()
                ?? string.Empty;

            port =
                Convert.ToInt32(
                    readerCorreo["Port"]);

            username =
                readerCorreo["Username"]?.ToString()?.Trim()
                ?? string.Empty;

            passwordEncriptado =
                readerCorreo["PasswordEncriptado"]?.ToString()
                ?? string.Empty;

            fromEmail =
                readerCorreo["FromEmail"]?.ToString()?.Trim()
                ?? string.Empty;

            fromName =
                readerCorreo["FromName"]?.ToString()?.Trim()
                ?? string.Empty;


            string password =
                _protector.Unprotect(
                    passwordEncriptado);


            // -------------------------------------------------
            // 3. Preparar enlace y contenido
            // -------------------------------------------------

            string enlaceDocumento =
                $"{Request.Scheme}://{Request.Host}{rutaArchivo}";


            string tituloSeguro =
                WebUtility.HtmlEncode(
                    titulo);

            string categoriaSegura =
                WebUtility.HtmlEncode(
                    nombreCategoria);

            string descripcionSegura =
                WebUtility.HtmlEncode(
                    string.IsNullOrWhiteSpace(
                        descripcion)
                        ? "Sin descripción."
                        : descripcion.Trim());

            string periodoSeguro =
                WebUtility.HtmlEncode(
                    string.IsNullOrWhiteSpace(
                        periodo)
                        ? "No disponible"
                        : periodo.Trim());

            string enlaceSeguro =
                WebUtility.HtmlEncode(
                    enlaceDocumento);


            // -------------------------------------------------
            // 4. Conectar una vez con SMTP
            // -------------------------------------------------

            using var smtp =
                new SmtpClient();


            await smtp.ConnectAsync(
                host,
                port,
                MailKit.Security.SecureSocketOptions.StartTls);


            await smtp.AuthenticateAsync(
                username,
                password);


            int enviados =
                0;

            int fallidos =
                0;


            // -------------------------------------------------
            // 5. Enviar una notificación por destinatario
            // -------------------------------------------------

            foreach (string correo in destinatarios)
            {
                try
                {
                    var mensaje =
                        new MimeMessage();


                    mensaje.From.Add(
                        new MailboxAddress(
                            fromName,
                            fromEmail));


                    mensaje.To.Add(
                        MailboxAddress.Parse(
                            correo));


                    mensaje.Subject =
                        $"Nuevo documento de {nombreCategoria} - ASADA SJM";


                    mensaje.Body =
                        new TextPart("html")
                        {
                            Text = $@"
                                <html>
                                <body style='font-family: Arial, sans-serif;'>

                                    <h2>
                                        ASADA San José de la Montaña
                                    </h2>

                                    <p>
                                        Se ha publicado un nuevo documento
                                        en una categoría a la que usted está suscrito.
                                    </p>

                                    <p>
                                        <strong>Categoría:</strong>
                                        {categoriaSegura}
                                    </p>

                                    <p>
                                        <strong>Documento:</strong>
                                        {tituloSeguro}
                                    </p>

                                    <p>
                                        <strong>Descripción:</strong>
                                        {descripcionSegura}
                                    </p>

                                    <p>
                                        <strong>Período:</strong>
                                        {periodoSeguro}
                                    </p>

                                    <p>
                                        <strong>Fecha de publicación:</strong>
                                        {fechaPublicacion:dd/MM/yyyy}
                                    </p>

                                    <p>
                                        <a href='{enlaceSeguro}'>
                                            Ver documento PDF
                                        </a>
                                    </p>

                                    <p style='color:#666;font-size:13px;'>
                                        Recibe este correo porque está suscrito
                                        a esta categoría en Transparencia Institucional.
                                    </p>

                                </body>
                                </html>"
                        };


                    await smtp.SendAsync(
                        mensaje);


                    enviados++;
                }
                catch
                {
                    fallidos++;
                }
            }


            await smtp.DisconnectAsync(
                true);


            return (
                Enviados: enviados,
                Fallidos: fallidos);
        }
        catch
        {
            return (
                Enviados: 0,
                Fallidos: destinatarios.Count);
        }
    }


    // =========================================================
    // CARGAR CATEGORÍAS
    // =========================================================

    private async Task CargarCategoriasAsync(
        DocumentoTransparenciaViewModel modelo)
    {
        modelo.Categorias.Clear();

        string? connectionString =
            _configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrWhiteSpace(
                connectionString))
        {
            return;
        }


        const string consulta = @"
            SELECT
                IdCategoria,
                Nombre,
                Descripcion,
                Estado
            FROM CATEGORIA_DOCUMENTO
            WHERE Estado = 1
            ORDER BY Nombre;";


        await using SqlConnection connection =
            new SqlConnection(
                connectionString);

        await using SqlCommand command =
            new SqlCommand(
                consulta,
                connection);

        await connection.OpenAsync();

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();


        while (await reader.ReadAsync())
        {
            modelo.Categorias.Add(
                new CategoriaDocumento
                {
                    IdCategoria =
                        Convert.ToInt32(
                            reader["IdCategoria"]),

                    Nombre =
                        reader["Nombre"]?.ToString()
                        ?? string.Empty,

                    Descripcion =
                        reader["Descripcion"] == DBNull.Value
                            ? null
                            : reader["Descripcion"]?.ToString(),

                    Estado =
                        Convert.ToBoolean(
                            reader["Estado"])
                });
        }
    }


    // =========================================================
    // VALIDAR CATEGORÍA
    // =========================================================

    private static async Task<bool>
        CategoriaExisteYEstaActivaAsync(
            int idCategoria,
            string connectionString)
    {
        const string consulta = @"
            SELECT COUNT(1)
            FROM CATEGORIA_DOCUMENTO
            WHERE
                IdCategoria = @IdCategoria
                AND Estado = 1;";


        await using SqlConnection connection =
            new SqlConnection(
                connectionString);

        await using SqlCommand command =
            new SqlCommand(
                consulta,
                connection);

        command.Parameters.Add(
            "@IdCategoria",
            SqlDbType.Int).Value =
            idCategoria;

        await connection.OpenAsync();

        int cantidad =
            Convert.ToInt32(
                await command.ExecuteScalarAsync());

        return cantidad > 0;
    }


    // =========================================================
    // VALIDAR CONTENIDO PDF
    // =========================================================

    private static async Task<bool>
        EsArchivoPdfValidoAsync(
            IFormFile archivo)
    {
        if (archivo.Length < 5)
        {
            return false;
        }


        byte[] encabezado =
            new byte[5];

        await using Stream stream =
            archivo.OpenReadStream();

        int bytesLeidos =
            await stream.ReadAsync(
                encabezado.AsMemory(
                    0,
                    encabezado.Length));


        if (bytesLeidos < 5)
        {
            return false;
        }


        string firma =
            Encoding.ASCII.GetString(
                encabezado);

        return firma == "%PDF-";
    }
}
