using AsadaSJM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;

namespace AsadaSJM.Controllers;

public class TransparenciaController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    private const long TamanoMaximoArchivo = 10 * 1024 * 1024;


    public TransparenciaController(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
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
            // 9. Registro exitoso
            // -------------------------------------------------

            TempData["MensajeExito"] =
                "El documento fue publicado correctamente.";

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