using AsadaSJM.Data;
using AsadaSJM.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using System.Data;
using System.Net.Mail;
using System.Text;

namespace AsadaSJM.Controllers;

[Authorize(Roles = "Administrador,Operativo")]
public class AbonadosController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    private const string ContrasenaInicialPruebas = "123456";

    private static readonly string[] EncabezadosRequeridos =
    {
        "Nombres",
        "PrimerApellido",
        "SegundoApellido",
        "Identificacion",
        "Telefono",
        "Correo",
        "NIS",
        "NumeroPaja",
        "NumeroFinca",
        "NumeroPlano",
        "Direccion",
        "Tarifa",
        "Sector"
    };

    public AbonadosController(
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }


    // =========================================================
    // LISTADO DE ABONADOS
    // =========================================================

    public async Task<IActionResult> Index(string? buscar)
    {
        var abonados = new List<Abonado>();

        string? connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return View(abonados);
        }

        using SqlConnection connection =
            new SqlConnection(connectionString);

        const string consulta = @"
            SELECT
                IdAbonado,
                IdUsuario,
                NIS,
                NumeroPaja,
                NumeroFinca,
                NumeroPlano,
                Direccion,
                IdTarifa,
                IdSector
            FROM ABONADO
            WHERE
                @Buscar IS NULL
                OR NIS LIKE @BuscarLike
                OR NumeroPaja LIKE @BuscarLike
                OR NumeroFinca LIKE @BuscarLike
                OR NumeroPlano LIKE @BuscarLike
                OR Direccion LIKE @BuscarLike
            ORDER BY NIS;";

        using SqlCommand command =
            new SqlCommand(consulta, connection);

        if (string.IsNullOrWhiteSpace(buscar))
        {
            command.Parameters.Add(
                "@Buscar",
                SqlDbType.VarChar,
                100).Value = DBNull.Value;

            command.Parameters.Add(
                "@BuscarLike",
                SqlDbType.VarChar,
                110).Value = DBNull.Value;
        }
        else
        {
            string textoBusqueda = buscar.Trim();

            command.Parameters.Add(
                "@Buscar",
                SqlDbType.VarChar,
                100).Value = textoBusqueda;

            command.Parameters.Add(
                "@BuscarLike",
                SqlDbType.VarChar,
                110).Value = $"%{textoBusqueda}%";
        }

        await connection.OpenAsync();

        using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            abonados.Add(
                new Abonado
                {
                    IdAbonado =
                        Convert.ToInt32(reader["IdAbonado"]),

                    IdUsuario =
                        Convert.ToInt32(reader["IdUsuario"]),

                    NIS =
                        reader["NIS"]?.ToString() ?? "",

                    NumeroPaja =
                        reader["NumeroPaja"]?.ToString() ?? "",

                    NumeroFinca =
                        reader["NumeroFinca"] == DBNull.Value
                            ? null
                            : reader["NumeroFinca"].ToString(),

                    NumeroPlano =
                        reader["NumeroPlano"] == DBNull.Value
                            ? null
                            : reader["NumeroPlano"].ToString(),

                    Direccion =
                        reader["Direccion"]?.ToString() ?? "",

                    IdTarifa =
                        Convert.ToInt32(reader["IdTarifa"]),

                    IdSector =
                        Convert.ToInt32(reader["IdSector"])
                });
        }

        return View(abonados);
    }


    // =========================================================
    // IMPORTAR ABONADOS - GET
    // =========================================================

    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public IActionResult Importar()
    {
        return View();
    }


    // =========================================================
    // IMPORTAR ABONADOS - POST
    // MU-02
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Importar(IFormFile? archivo)
    {
        // -----------------------------------------------------
        // 1. Validar archivo
        // -----------------------------------------------------

        if (archivo == null || archivo.Length == 0)
        {
            ModelState.AddModelError(
                string.Empty,
                "Debe seleccionar un archivo.");

            return View();
        }

        if (archivo.Length > 10 * 1024 * 1024)
        {
            ModelState.AddModelError(
                string.Empty,
                "El archivo no puede superar los 10 MB.");

            return View();
        }

        string extension =
            Path.GetExtension(archivo.FileName)
                .ToLowerInvariant();

        if (extension != ".xlsx" &&
            extension != ".csv")
        {
            ModelState.AddModelError(
                string.Empty,
                "Solo se permiten archivos Excel (.xlsx) o CSV (.csv).");

            return View();
        }

        // -----------------------------------------------------
        // 2. Leer archivo
        // -----------------------------------------------------

        List<FilaImportacionAbonado> filas;

        try
        {
            filas = extension == ".xlsx"
                ? LeerExcel(archivo)
                : await LeerCsvAsync(archivo);
        }
        catch (InvalidDataException ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            return View();
        }
        catch
        {
            ModelState.AddModelError(
                string.Empty,
                "No fue posible leer el archivo seleccionado.");

            return View();
        }

        if (filas.Count == 0)
        {
            ModelState.AddModelError(
                string.Empty,
                "El archivo no contiene abonados para importar.");

            return View();
        }

        // -----------------------------------------------------
        // 3. Obtener conexión
        // -----------------------------------------------------

        string? connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            ModelState.AddModelError(
                string.Empty,
                "No se encontró la conexión a la base de datos.");

            return View();
        }

        using SqlConnection connection =
            new SqlConnection(connectionString);

        await connection.OpenAsync();

        // -----------------------------------------------------
        // 4. Cargar catálogos
        // -----------------------------------------------------

        Dictionary<string, int> tarifas =
            await CargarCatalogoAsync(
                connection,
                "SELECT IdTarifa, Nombre FROM TARIFA WHERE Estado = 1;",
                "IdTarifa");

        Dictionary<string, int> sectores =
            await CargarCatalogoAsync(
                connection,
                "SELECT IdSector, Nombre FROM SECTOR WHERE Estado = 1;",
                "IdSector");

        // -----------------------------------------------------
        // 5. Verificar ROL-ABONADO
        // -----------------------------------------------------

        const string consultaRol = @"
            SELECT COUNT(*)
            FROM AspNetRoles
            WHERE Id = 'ROL-ABONADO'
              AND Name = 'Abonado';";

        using (SqlCommand commandRol =
            new SqlCommand(consultaRol, connection))
        {
            int existeRol =
                Convert.ToInt32(
                    await commandRol.ExecuteScalarAsync());

            if (existeRol == 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No se encontró el rol Abonado en la base de datos.");

                return View();
            }
        }

        // -----------------------------------------------------
        // 6. Cargar datos ya existentes
        // -----------------------------------------------------

        HashSet<string> correosExistentes =
            await CargarValoresAsync(
                connection,
                @"
                SELECT Email
                FROM AspNetUsers
                WHERE Email IS NOT NULL

                UNION

                SELECT CorreoElectronico
                FROM USUARIO
                WHERE CorreoElectronico IS NOT NULL;");

        HashSet<string> identificacionesExistentes =
            await CargarValoresAsync(
                connection,
                "SELECT Identificacion FROM USUARIO;");

        HashSet<string> nisExistentes =
            await CargarValoresAsync(
                connection,
                "SELECT NIS FROM ABONADO;");

        HashSet<string> pajasExistentes =
            await CargarValoresAsync(
                connection,
                "SELECT NumeroPaja FROM ABONADO;");

        // -----------------------------------------------------
        // 7. Validar todas las filas antes de insertar
        // -----------------------------------------------------

        var errores = new List<string>();

        var correosArchivo =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        var identificacionesArchivo =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        var nisArchivo =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        var pajasArchivo =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (FilaImportacionAbonado fila in filas)
        {
            ValidarFilaBasica(
                fila,
                errores);

            if (!string.IsNullOrWhiteSpace(fila.Correo))
            {
                if (correosExistentes.Contains(fila.Correo))
                {
                    errores.Add(
                        $"Fila {fila.NumeroFila}: el correo '{fila.Correo}' ya está registrado.");
                }

                if (!correosArchivo.Add(fila.Correo))
                {
                    errores.Add(
                        $"Fila {fila.NumeroFila}: el correo '{fila.Correo}' está repetido dentro del archivo.");
                }
            }

            if (!string.IsNullOrWhiteSpace(fila.Identificacion))
            {
                if (identificacionesExistentes.Contains(
                    fila.Identificacion))
                {
                    errores.Add(
                        $"Fila {fila.NumeroFila}: la identificación '{fila.Identificacion}' ya está registrada.");
                }

                if (!identificacionesArchivo.Add(
                    fila.Identificacion))
                {
                    errores.Add(
                        $"Fila {fila.NumeroFila}: la identificación '{fila.Identificacion}' está repetida dentro del archivo.");
                }
            }

            if (!string.IsNullOrWhiteSpace(fila.NIS))
            {
                if (nisExistentes.Contains(fila.NIS))
                {
                    errores.Add(
                        $"Fila {fila.NumeroFila}: el NIS '{fila.NIS}' ya está registrado.");
                }

                if (!nisArchivo.Add(fila.NIS))
                {
                    errores.Add(
                        $"Fila {fila.NumeroFila}: el NIS '{fila.NIS}' está repetido dentro del archivo.");
                }
            }

            if (!string.IsNullOrWhiteSpace(fila.NumeroPaja))
            {
                if (pajasExistentes.Contains(
                    fila.NumeroPaja))
                {
                    errores.Add(
                        $"Fila {fila.NumeroFila}: el número de paja '{fila.NumeroPaja}' ya está registrado.");
                }

                if (!pajasArchivo.Add(
                    fila.NumeroPaja))
                {
                    errores.Add(
                        $"Fila {fila.NumeroFila}: el número de paja '{fila.NumeroPaja}' está repetido dentro del archivo.");
                }
            }

            if (!string.IsNullOrWhiteSpace(fila.Tarifa) &&
                !tarifas.ContainsKey(fila.Tarifa))
            {
                errores.Add(
                    $"Fila {fila.NumeroFila}: la tarifa '{fila.Tarifa}' no existe o está inactiva.");
            }

            if (!string.IsNullOrWhiteSpace(fila.Sector) &&
                !sectores.ContainsKey(fila.Sector))
            {
                errores.Add(
                    $"Fila {fila.NumeroFila}: el sector '{fila.Sector}' no existe o está inactivo.");
            }
        }

        if (errores.Count > 0)
        {
            foreach (string error in errores.Take(20))
            {
                ModelState.AddModelError(
                    string.Empty,
                    error);
            }

            if (errores.Count > 20)
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"Se encontraron {errores.Count} errores. Se muestran los primeros 20.");
            }

            return View();
        }

        // -----------------------------------------------------
        // 8. Iniciar transacción
        // -----------------------------------------------------

        using SqlTransaction transaction =
            connection.BeginTransaction();

        try
        {
            int importados = 0;

            foreach (FilaImportacionAbonado fila in filas)
            {
                string idNetUser =
                    Guid.NewGuid().ToString();

                string passwordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        ContrasenaInicialPruebas);

                string securityStamp =
                    Guid.NewGuid().ToString();

                // ---------------------------------------------
                // AspNetUsers
                // ---------------------------------------------

                const string insertarAspNetUser = @"
                    INSERT INTO AspNetUsers
                    (
                        Id,
                        Email,
                        EmailConfirmed,
                        PasswordHash,
                        SecurityStamp,
                        PhoneNumber,
                        PhoneNumberConfirmed,
                        TwoFactorEnabled,
                        LockoutEndDateUtc,
                        LockoutEnabled,
                        AccessFailedCount,
                        UserName
                    )
                    VALUES
                    (
                        @Id,
                        @Correo,
                        1,
                        @PasswordHash,
                        @SecurityStamp,
                        @Telefono,
                        0,
                        0,
                        NULL,
                        1,
                        0,
                        @Correo
                    );";

                using (SqlCommand command =
                    new SqlCommand(
                        insertarAspNetUser,
                        connection,
                        transaction))
                {
                    command.Parameters.Add(
                        "@Id",
                        SqlDbType.NVarChar,
                        128).Value = idNetUser;

                    command.Parameters.Add(
                        "@Correo",
                        SqlDbType.NVarChar,
                        256).Value = fila.Correo;

                    command.Parameters.Add(
                        "@PasswordHash",
                        SqlDbType.NVarChar,
                        -1).Value = passwordHash;

                    command.Parameters.Add(
                        "@SecurityStamp",
                        SqlDbType.NVarChar,
                        -1).Value = securityStamp;

                    command.Parameters.Add(
                        "@Telefono",
                        SqlDbType.NVarChar,
                        -1).Value = fila.Telefono;

                    await command.ExecuteNonQueryAsync();
                }

                // ---------------------------------------------
                // USUARIO
                // ---------------------------------------------

                const string insertarUsuario = @"
                    INSERT INTO USUARIO
                    (
                        IdNetUser,
                        Nombres,
                        PrimerApellido,
                        SegundoApellido,
                        Identificacion,
                        CorreoElectronico,
                        Telefono,
                        FechaDeRegistro,
                        FechaDeModificacion,
                        Estado
                    )
                    OUTPUT INSERTED.IdUsuario
                    VALUES
                    (
                        @IdNetUser,
                        @Nombres,
                        @PrimerApellido,
                        @SegundoApellido,
                        @Identificacion,
                        @Correo,
                        @Telefono,
                        GETDATE(),
                        NULL,
                        1
                    );";

                int idUsuario;

                using (SqlCommand command =
                    new SqlCommand(
                        insertarUsuario,
                        connection,
                        transaction))
                {
                    command.Parameters.Add(
                        "@IdNetUser",
                        SqlDbType.NVarChar,
                        128).Value = idNetUser;

                    command.Parameters.Add(
                        "@Nombres",
                        SqlDbType.VarChar,
                        100).Value = fila.Nombres;

                    command.Parameters.Add(
                        "@PrimerApellido",
                        SqlDbType.VarChar,
                        100).Value = fila.PrimerApellido;

                    command.Parameters.Add(
                        "@SegundoApellido",
                        SqlDbType.VarChar,
                        100).Value = fila.SegundoApellido;

                    command.Parameters.Add(
                        "@Identificacion",
                        SqlDbType.VarChar,
                        20).Value = fila.Identificacion;

                    command.Parameters.Add(
                        "@Correo",
                        SqlDbType.VarChar,
                        200).Value = fila.Correo;

                    command.Parameters.Add(
                        "@Telefono",
                        SqlDbType.VarChar,
                        20).Value = fila.Telefono;

                    idUsuario =
                        Convert.ToInt32(
                            await command.ExecuteScalarAsync());
                }

                // ---------------------------------------------
                // AspNetUserRoles
                // ---------------------------------------------

                const string insertarRol = @"
                    INSERT INTO AspNetUserRoles
                    (
                        UserId,
                        RoleId
                    )
                    VALUES
                    (
                        @UserId,
                        'ROL-ABONADO'
                    );";

                using (SqlCommand command =
                    new SqlCommand(
                        insertarRol,
                        connection,
                        transaction))
                {
                    command.Parameters.Add(
                        "@UserId",
                        SqlDbType.NVarChar,
                        128).Value = idNetUser;

                    await command.ExecuteNonQueryAsync();
                }

                // ---------------------------------------------
                // ABONADO
                // ---------------------------------------------

                const string insertarAbonado = @"
                    INSERT INTO ABONADO
                    (
                        IdUsuario,
                        NIS,
                        NumeroPaja,
                        NumeroFinca,
                        NumeroPlano,
                        Direccion,
                        IdTarifa,
                        IdSector,
                        Estado
                    )
                    VALUES
                    (
                        @IdUsuario,
                        @NIS,
                        @NumeroPaja,
                        @NumeroFinca,
                        @NumeroPlano,
                        @Direccion,
                        @IdTarifa,
                        @IdSector,
                        1
                    );";

                using (SqlCommand command =
                    new SqlCommand(
                        insertarAbonado,
                        connection,
                        transaction))
                {
                    command.Parameters.Add(
                        "@IdUsuario",
                        SqlDbType.Int).Value = idUsuario;

                    command.Parameters.Add(
                        "@NIS",
                        SqlDbType.VarChar,
                        30).Value = fila.NIS;

                    command.Parameters.Add(
                        "@NumeroPaja",
                        SqlDbType.VarChar,
                        30).Value = fila.NumeroPaja;

                    command.Parameters.Add(
                        "@NumeroFinca",
                        SqlDbType.VarChar,
                        30).Value =
                            string.IsNullOrWhiteSpace(
                                fila.NumeroFinca)
                                ? DBNull.Value
                                : fila.NumeroFinca;

                    command.Parameters.Add(
                        "@NumeroPlano",
                        SqlDbType.VarChar,
                        30).Value =
                            string.IsNullOrWhiteSpace(
                                fila.NumeroPlano)
                                ? DBNull.Value
                                : fila.NumeroPlano;

                    command.Parameters.Add(
                        "@Direccion",
                        SqlDbType.VarChar,
                        500).Value = fila.Direccion;

                    command.Parameters.Add(
                        "@IdTarifa",
                        SqlDbType.Int).Value =
                            tarifas[fila.Tarifa];

                    command.Parameters.Add(
                        "@IdSector",
                        SqlDbType.Int).Value =
                            sectores[fila.Sector];

                    await command.ExecuteNonQueryAsync();
                }

                importados++;
            }

            transaction.Commit();

            TempData["ImportacionExitosa"] =
                $"Se importaron correctamente {importados} abonado(s). " +
                $"La contraseña inicial de prueba es {ContrasenaInicialPruebas}.";

            return RedirectToAction(nameof(Index));
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

            ModelState.AddModelError(
                string.Empty,
                "Ocurrió un error durante la importación. No se guardó ningún abonado.");

            return View();
        }
    }


    // =========================================================
    // REGISTRAR ABONADO
    // =========================================================

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("IdUsuario,NIS,NumeroPaja,NumeroFinca,NumeroPlano,Direccion,IdTarifa,IdSector")]
        Abonado abonado)
    {
        if (!ModelState.IsValid)
        {
            return View(abonado);
        }

        _context.Add(abonado);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }


    // =========================================================
    // EDITAR ABONADO
    // =========================================================

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var abonado =
            await _context.Abonados.FindAsync(id);

        if (abonado is null)
        {
            return NotFound();
        }

        return View(abonado);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("IdAbonado,IdUsuario,NIS,NumeroPaja,NumeroFinca,NumeroPlano,Direccion,IdTarifa,IdSector")]
        Abonado abonado)
    {
        if (id != abonado.IdAbonado)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(abonado);
        }

        _context.Update(abonado);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }


    // =========================================================
    // ELIMINAR ABONADO
    // =========================================================

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var abonado =
            await _context.Abonados
                .FirstOrDefaultAsync(
                    a => a.IdAbonado == id);

        if (abonado is null)
        {
            return NotFound();
        }

        return View(abonado);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var abonado =
            await _context.Abonados.FindAsync(id);

        if (abonado is not null)
        {
            _context.Abonados.Remove(abonado);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }


    // =========================================================
    // LEER EXCEL
    // =========================================================

    private static List<FilaImportacionAbonado> LeerExcel(
        IFormFile archivo)
    {
        var filas =
            new List<FilaImportacionAbonado>();

        using Stream stream =
            archivo.OpenReadStream();

        using var workbook =
            new XLWorkbook(stream);

        var worksheet =
            workbook.Worksheets.FirstOrDefault();

        if (worksheet == null)
        {
            throw new InvalidDataException(
                "El archivo Excel no contiene hojas.");
        }

        var mapa =
            new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var celda in
            worksheet.Row(1).CellsUsed())
        {
            string encabezado =
                celda.GetString().Trim();

            if (string.IsNullOrWhiteSpace(encabezado))
            {
                continue;
            }

            if (mapa.ContainsKey(encabezado))
            {
                throw new InvalidDataException(
                    $"El encabezado '{encabezado}' está repetido.");
            }

            mapa[encabezado] =
                celda.Address.ColumnNumber;
        }

        ValidarEncabezados(mapa.Keys);

        int ultimaFila =
            worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int numeroFila = 2;
             numeroFila <= ultimaFila;
             numeroFila++)
        {
            var fila =
                new FilaImportacionAbonado
                {
                    NumeroFila = numeroFila,

                    Nombres =
                        ObtenerExcel(
                            worksheet,
                            numeroFila,
                            mapa,
                            "Nombres"),

                    PrimerApellido =
                        ObtenerExcel(
                            worksheet,
                            numeroFila,
                            mapa,
                            "PrimerApellido"),

                    SegundoApellido =
                        ObtenerExcel(
                            worksheet,
                            numeroFila,
                            mapa,
                            "SegundoApellido"),

                    Identificacion =
                        ObtenerExcel(
                            worksheet,
                            numeroFila,
                            mapa,
                            "Identificacion"),

                    Telefono =
                        ObtenerExcel(
                            worksheet,
                            numeroFila,
                            mapa,
                            "Telefono"),

                    Correo =
                        ObtenerExcel(
                            worksheet,
                            numeroFila,
                            mapa,
                            "Correo")
                            .ToLowerInvariant(),

                    NIS =
                        ObtenerExcel(
                            worksheet,
                            numeroFila,
                            mapa,
                            "NIS"),

                    NumeroPaja =
                        ObtenerExcel(
                            worksheet,
                            numeroFila,
                            mapa,
                            "NumeroPaja"),

                    NumeroFinca =
                        ObtenerExcel(
                            worksheet,
                            numeroFila,
                            mapa,
                            "NumeroFinca"),

                    NumeroPlano =
                        ObtenerExcel(
                            worksheet,
                            numeroFila,
                            mapa,
                            "NumeroPlano"),

                    Direccion =
                        ObtenerExcel(
                            worksheet,
                            numeroFila,
                            mapa,
                            "Direccion"),

                    Tarifa =
                        ObtenerExcel(
                            worksheet,
                            numeroFila,
                            mapa,
                            "Tarifa"),

                    Sector =
                        ObtenerExcel(
                            worksheet,
                            numeroFila,
                            mapa,
                            "Sector")
                };

            if (!FilaVacia(fila))
            {
                filas.Add(fila);
            }
        }

        return filas;
    }


    // =========================================================
    // LEER CSV
    // =========================================================

    private static async Task<List<FilaImportacionAbonado>>
        LeerCsvAsync(IFormFile archivo)
    {
        string delimitador;

        using (StreamReader reader =
            new StreamReader(
                archivo.OpenReadStream(),
                Encoding.UTF8,
                true))
        {
            string primeraLinea =
                await reader.ReadLineAsync() ?? "";

            int comas =
                primeraLinea.Count(c => c == ',');

            int puntoComas =
                primeraLinea.Count(c => c == ';');

            delimitador =
                puntoComas > comas
                    ? ";"
                    : ",";
        }

        var filas =
            new List<FilaImportacionAbonado>();

        using var parser =
            new TextFieldParser(
                archivo.OpenReadStream(),
                Encoding.UTF8,
                true);

        parser.TextFieldType =
            FieldType.Delimited;

        parser.SetDelimiters(delimitador);

        parser.HasFieldsEnclosedInQuotes = true;
        parser.TrimWhiteSpace = true;

        string[]? encabezados =
            parser.ReadFields();

        if (encabezados == null)
        {
            throw new InvalidDataException(
                "El archivo CSV no contiene encabezados.");
        }

        var mapa =
            new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

        for (int i = 0;
             i < encabezados.Length;
             i++)
        {
            string encabezado =
                encabezados[i].Trim();

            if (string.IsNullOrWhiteSpace(encabezado))
            {
                continue;
            }

            if (mapa.ContainsKey(encabezado))
            {
                throw new InvalidDataException(
                    $"El encabezado '{encabezado}' está repetido.");
            }

            mapa[encabezado] = i;
        }

        ValidarEncabezados(mapa.Keys);

        int numeroFila = 1;

        while (!parser.EndOfData)
        {
            numeroFila++;

            string[]? campos =
                parser.ReadFields();

            if (campos == null)
            {
                continue;
            }

            var fila =
                new FilaImportacionAbonado
                {
                    NumeroFila = numeroFila,

                    Nombres =
                        ObtenerCsv(
                            campos,
                            mapa,
                            "Nombres"),

                    PrimerApellido =
                        ObtenerCsv(
                            campos,
                            mapa,
                            "PrimerApellido"),

                    SegundoApellido =
                        ObtenerCsv(
                            campos,
                            mapa,
                            "SegundoApellido"),

                    Identificacion =
                        ObtenerCsv(
                            campos,
                            mapa,
                            "Identificacion"),

                    Telefono =
                        ObtenerCsv(
                            campos,
                            mapa,
                            "Telefono"),

                    Correo =
                        ObtenerCsv(
                            campos,
                            mapa,
                            "Correo")
                            .ToLowerInvariant(),

                    NIS =
                        ObtenerCsv(
                            campos,
                            mapa,
                            "NIS"),

                    NumeroPaja =
                        ObtenerCsv(
                            campos,
                            mapa,
                            "NumeroPaja"),

                    NumeroFinca =
                        ObtenerCsv(
                            campos,
                            mapa,
                            "NumeroFinca"),

                    NumeroPlano =
                        ObtenerCsv(
                            campos,
                            mapa,
                            "NumeroPlano"),

                    Direccion =
                        ObtenerCsv(
                            campos,
                            mapa,
                            "Direccion"),

                    Tarifa =
                        ObtenerCsv(
                            campos,
                            mapa,
                            "Tarifa"),

                    Sector =
                        ObtenerCsv(
                            campos,
                            mapa,
                            "Sector")
                };

            if (!FilaVacia(fila))
            {
                filas.Add(fila);
            }
        }

        return filas;
    }


    // =========================================================
    // VALIDACIONES
    // =========================================================

    private static void ValidarEncabezados(
        IEnumerable<string> encabezados)
    {
        var existentes =
            new HashSet<string>(
                encabezados,
                StringComparer.OrdinalIgnoreCase);

        var faltantes =
            EncabezadosRequeridos
                .Where(e => !existentes.Contains(e))
                .ToList();

        if (faltantes.Count > 0)
        {
            throw new InvalidDataException(
                "Faltan las siguientes columnas: " +
                string.Join(", ", faltantes) +
                ".");
        }
    }

    private static void ValidarFilaBasica(
        FilaImportacionAbonado fila,
        List<string> errores)
    {
        if (string.IsNullOrWhiteSpace(fila.Nombres))
            errores.Add(
                $"Fila {fila.NumeroFila}: Nombres es obligatorio.");

        if (string.IsNullOrWhiteSpace(fila.PrimerApellido))
            errores.Add(
                $"Fila {fila.NumeroFila}: PrimerApellido es obligatorio.");

        if (string.IsNullOrWhiteSpace(fila.SegundoApellido))
            errores.Add(
                $"Fila {fila.NumeroFila}: SegundoApellido es obligatorio.");

        if (string.IsNullOrWhiteSpace(fila.Identificacion))
            errores.Add(
                $"Fila {fila.NumeroFila}: Identificacion es obligatoria.");

        if (string.IsNullOrWhiteSpace(fila.Telefono))
            errores.Add(
                $"Fila {fila.NumeroFila}: Telefono es obligatorio.");

        if (string.IsNullOrWhiteSpace(fila.Correo))
        {
            errores.Add(
                $"Fila {fila.NumeroFila}: Correo es obligatorio.");
        }
        else if (!CorreoValido(fila.Correo))
        {
            errores.Add(
                $"Fila {fila.NumeroFila}: el correo '{fila.Correo}' no es válido.");
        }

        if (string.IsNullOrWhiteSpace(fila.NIS))
            errores.Add(
                $"Fila {fila.NumeroFila}: NIS es obligatorio.");

        if (string.IsNullOrWhiteSpace(fila.NumeroPaja))
            errores.Add(
                $"Fila {fila.NumeroFila}: NumeroPaja es obligatorio.");

        if (string.IsNullOrWhiteSpace(fila.Direccion))
            errores.Add(
                $"Fila {fila.NumeroFila}: Direccion es obligatoria.");

        if (string.IsNullOrWhiteSpace(fila.Tarifa))
            errores.Add(
                $"Fila {fila.NumeroFila}: Tarifa es obligatoria.");

        if (string.IsNullOrWhiteSpace(fila.Sector))
            errores.Add(
                $"Fila {fila.NumeroFila}: Sector es obligatorio.");

        ValidarLongitud(
            fila.Nombres,
            100,
            "Nombres",
            fila.NumeroFila,
            errores);

        ValidarLongitud(
            fila.PrimerApellido,
            100,
            "PrimerApellido",
            fila.NumeroFila,
            errores);

        ValidarLongitud(
            fila.SegundoApellido,
            100,
            "SegundoApellido",
            fila.NumeroFila,
            errores);

        ValidarLongitud(
            fila.Identificacion,
            20,
            "Identificacion",
            fila.NumeroFila,
            errores);

        ValidarLongitud(
            fila.Telefono,
            20,
            "Telefono",
            fila.NumeroFila,
            errores);

        ValidarLongitud(
            fila.Correo,
            200,
            "Correo",
            fila.NumeroFila,
            errores);

        ValidarLongitud(
            fila.NIS,
            30,
            "NIS",
            fila.NumeroFila,
            errores);

        ValidarLongitud(
            fila.NumeroPaja,
            30,
            "NumeroPaja",
            fila.NumeroFila,
            errores);

        ValidarLongitud(
            fila.NumeroFinca,
            30,
            "NumeroFinca",
            fila.NumeroFila,
            errores);

        ValidarLongitud(
            fila.NumeroPlano,
            30,
            "NumeroPlano",
            fila.NumeroFila,
            errores);

        ValidarLongitud(
            fila.Direccion,
            500,
            "Direccion",
            fila.NumeroFila,
            errores);
    }

    private static void ValidarLongitud(
        string? valor,
        int longitudMaxima,
        string campo,
        int numeroFila,
        List<string> errores)
    {
        if (!string.IsNullOrEmpty(valor) &&
            valor.Length > longitudMaxima)
        {
            errores.Add(
                $"Fila {numeroFila}: {campo} supera los {longitudMaxima} caracteres permitidos.");
        }
    }

    private static bool CorreoValido(
        string correo)
    {
        try
        {
            var direccion =
                new MailAddress(correo);

            return string.Equals(
                direccion.Address,
                correo,
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }


    // =========================================================
    // AYUDANTES PARA ARCHIVOS
    // =========================================================

    private static string ObtenerExcel(
        IXLWorksheet worksheet,
        int fila,
        Dictionary<string, int> mapa,
        string columna)
    {
        return worksheet
            .Cell(fila, mapa[columna])
            .GetFormattedString()
            .Trim();
    }

    private static string ObtenerCsv(
        string[] campos,
        Dictionary<string, int> mapa,
        string columna)
    {
        int indice =
            mapa[columna];

        if (indice >= campos.Length)
        {
            return string.Empty;
        }

        return campos[indice].Trim();
    }

    private static bool FilaVacia(
        FilaImportacionAbonado fila)
    {
        return
            string.IsNullOrWhiteSpace(fila.Nombres) &&
            string.IsNullOrWhiteSpace(fila.PrimerApellido) &&
            string.IsNullOrWhiteSpace(fila.SegundoApellido) &&
            string.IsNullOrWhiteSpace(fila.Identificacion) &&
            string.IsNullOrWhiteSpace(fila.Telefono) &&
            string.IsNullOrWhiteSpace(fila.Correo) &&
            string.IsNullOrWhiteSpace(fila.NIS) &&
            string.IsNullOrWhiteSpace(fila.NumeroPaja) &&
            string.IsNullOrWhiteSpace(fila.NumeroFinca) &&
            string.IsNullOrWhiteSpace(fila.NumeroPlano) &&
            string.IsNullOrWhiteSpace(fila.Direccion) &&
            string.IsNullOrWhiteSpace(fila.Tarifa) &&
            string.IsNullOrWhiteSpace(fila.Sector);
    }


    // =========================================================
    // AYUDANTES PARA BASE DE DATOS
    // =========================================================

    private static async Task<Dictionary<string, int>>
        CargarCatalogoAsync(
            SqlConnection connection,
            string consulta,
            string columnaId)
    {
        var resultado =
            new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

        using SqlCommand command =
            new SqlCommand(
                consulta,
                connection);

        using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            int id =
                Convert.ToInt32(
                    reader[columnaId]);

            string nombre =
                reader["Nombre"]?.ToString()?.Trim()
                ?? "";

            if (!string.IsNullOrWhiteSpace(nombre))
            {
                resultado[nombre] = id;
            }
        }

        return resultado;
    }

    private static async Task<HashSet<string>>
        CargarValoresAsync(
            SqlConnection connection,
            string consulta)
    {
        var resultado =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        using SqlCommand command =
            new SqlCommand(
                consulta,
                connection);

        using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            if (!reader.IsDBNull(0))
            {
                string valor =
                    reader.GetValue(0)
                        .ToString()?
                        .Trim()
                    ?? "";

                if (!string.IsNullOrWhiteSpace(valor))
                {
                    resultado.Add(valor);
                }
            }
        }

        return resultado;
    }


    // =========================================================
    // MODELO INTERNO DE IMPORTACIÓN
    // =========================================================

    private sealed class FilaImportacionAbonado
    {
        public int NumeroFila { get; set; }

        public string Nombres { get; set; } = "";
        public string PrimerApellido { get; set; } = "";
        public string SegundoApellido { get; set; } = "";
        public string Identificacion { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Correo { get; set; } = "";

        public string NIS { get; set; } = "";
        public string NumeroPaja { get; set; } = "";
        public string NumeroFinca { get; set; } = "";
        public string NumeroPlano { get; set; } = "";
        public string Direccion { get; set; } = "";

        public string Tarifa { get; set; } = "";
        public string Sector { get; set; } = "";
    }
}