
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.Text;

namespace AsadaSJM.Controllers;

[Authorize(Roles = "Administrador")]
public class PanelAdministrativoController : Controller
{
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    private const long MaxPdf = 10 * 1024 * 1024;
    private const long MaxImagen = 5 * 1024 * 1024;

    public PanelAdministrativoController(
        IConfiguration config,
        IWebHostEnvironment env)
    {
        _config = config;
        _env = env;
    }

    private SqlConnection Conexion() => new(
        _config.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Falta la conexión DefaultConnection."));

    private static SqlParameter P(string clave, object? valor) =>
        new(clave, valor ?? DBNull.Value);

    private async Task<DataTable> Consultar(
        string sql,
        params SqlParameter[] parametros)
    {
        await using var cn = Conexion();
        await cn.OpenAsync();

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddRange(parametros);

        await using var reader = await cmd.ExecuteReaderAsync();

        var tabla = new DataTable();
        tabla.Load(reader);

        return tabla;
    }

    private async Task<int> Ejecutar(
        string sql,
        params SqlParameter[] parametros)
    {
        await using var cn = Conexion();
        await cn.OpenAsync();

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddRange(parametros);

        return await cmd.ExecuteNonQueryAsync();
    }

    private IActionResult Volver(
        string tab,
        int? idAbonado = null) =>
        RedirectToAction(
            nameof(Index),
            new { tab, idAbonado });

    private static string Col(string nombre) =>
        "[" + nombre.Replace("]", "]]", StringComparison.Ordinal) + "]";

    private async Task<HashSet<string>> ColumnasTramite()
    {
        var tabla = await Consultar(@"
            SELECT name
            FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.TRAMITE');");

        return tabla.Rows.Cast<DataRow>()
            .Select(f => f[0].ToString() ?? "")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string? Elegir(
        HashSet<string> columnas,
        params string[] nombres) =>
        nombres.FirstOrDefault(columnas.Contains);

    // =====================================================
    // PANTALLAS DEL PANEL ADMINISTRATIVO
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Index(
        string tab = "noticias",
        string? tipo = null,
        string? estado = null,
        bool pendientes = false,
        int? idAbonado = null)
    {
        string[] secciones =
        {
            "noticias",
            "documentos",
            "tramites",
            "averias",
            "abonados"
        };

        tab = secciones.Contains(
            tab,
            StringComparer.OrdinalIgnoreCase)
                ? tab.ToLowerInvariant()
                : "noticias";

        ViewBag.Tab = tab;
        ViewBag.Tipo = tipo;
        ViewBag.Estado = estado;
        ViewBag.Pendientes = pendientes;

        ViewBag.Noticias = new DataTable();
        ViewBag.Documentos = new DataTable();
        ViewBag.Versiones = new DataTable();
        ViewBag.Tramites = new DataTable();
        ViewBag.Fontaneros = new DataTable();
        ViewBag.Averias = new DataTable();
        ViewBag.Ordenes = new DataTable();
        ViewBag.Abonados = new DataTable();
        ViewBag.Tarifas = new DataTable();
        ViewBag.AbonadoSeleccionado = new DataTable();
        ViewBag.Cambios = new DataTable();
        ViewBag.Contratos = new DataTable();

        // MPA-02: NOTICIAS

        if (tab == "noticias")
        {
            ViewBag.Noticias = await Consultar(@"
                SELECT
                    IdNoticia,
                    Titulo,
                    Contenido,
                    RutaImagen,
                    Estado,
                    FechaVigencia,
                    FechaCreacion,
                    CASE
                        WHEN Estado = 'Publicado'
                         AND CONVERT(date, FechaVigencia)
                             < CONVERT(date, GETDATE())
                        THEN 1
                        ELSE 0
                    END AS Vencida
                FROM dbo.MPA_NOTICIA
                ORDER BY FechaCreacion DESC;");
        }

        // MPA-03: DOCUMENTOS

        else if (tab == "documentos")
        {
            ViewBag.Documentos = await Consultar(@"
                SELECT
                    d.IdDocumentoControl,
                    d.Titulo,
                    d.TipoDocumento,
                    d.Descripcion,
                    d.Activo,
                    COUNT(v.IdVersion) AS Versiones,
                    MAX(v.FechaActualizacion)
                        AS UltimaActualizacion
                FROM dbo.MPA_DOCUMENTO_CONTROL d
                LEFT JOIN dbo.MPA_DOCUMENTO_VERSION v
                    ON v.IdDocumentoControl =
                       d.IdDocumentoControl
                GROUP BY
                    d.IdDocumentoControl,
                    d.Titulo,
                    d.TipoDocumento,
                    d.Descripcion,
                    d.Activo
                ORDER BY d.IdDocumentoControl DESC;");

            ViewBag.Versiones = await Consultar(@"
                SELECT
                    v.IdVersion,
                    d.Titulo,
                    v.NumeroVersion,
                    v.NombreArchivo,
                    v.FechaActualizacion,
                    v.Responsable,
                    v.Cargo,
                    v.FirmaValidada
                FROM dbo.MPA_DOCUMENTO_VERSION v
                JOIN dbo.MPA_DOCUMENTO_CONTROL d
                    ON d.IdDocumentoControl =
                       v.IdDocumentoControl
                ORDER BY
                    d.IdDocumentoControl DESC,
                    v.NumeroVersion DESC;");
        }

        // MPA-04: TRÁMITES

        else if (tab == "tramites")
        {
            var columnas = await ColumnasTramite();

            var colFecha = Elegir(
                columnas,
                "FechaSolicitud",
                "FechaRegistro",
                "FechaCreacion",
                "FechaTramite",
                "Fecha");

            var colEstado = Elegir(
                columnas,
                "Estado",
                "EstadoTramite",
                "Estatus");

            if (!columnas.Contains("IdTramite"))
            {
                TempData["Error"] =
                    "No existe IdTramite en la base de datos.";
            }
            else
            {
                var tipoSql = columnas.Contains("IdTipoTramite")
                    ? "COALESCE(tt.Nombre,N'Tipo no registrado')"
                    : "N'Tipo no registrado'";

                var joinTipo = columnas.Contains("IdTipoTramite")
                    ? @"LEFT JOIN dbo.TIPO_TRAMITE tt
                        ON tt.IdTipoTramite = t.IdTipoTramite"
                    : "";

                var estadoSql = colEstado == null
                    ? "N'No registrado'"
                    : $"CONVERT(nvarchar(50),t.{Col(colEstado)})";

                var fechaSql = colFecha == null
                    ? "CAST(NULL AS datetime2(0))"
                    : $"TRY_CONVERT(datetime2(0),t.{Col(colFecha)})";

                var consulta = $@"
                    SELECT
                        q.TipoRegistro,
                        q.IdRegistro,
                        q.Tipo,
                        q.Estado,
                        q.FechaRecepcion,
                        q.EsAveria,
                        q.EstadoReporte,
                        COALESCE(
                            o.CanalOrigen,
                            N'No registrado'
                        ) AS CanalOrigen
                    FROM
                    (
                        SELECT
                            N'Tramite' AS TipoRegistro,
                            t.IdTramite AS IdRegistro,
                            {tipoSql} AS Tipo,
                            {estadoSql} AS Estado,
                            {fechaSql} AS FechaRecepcion,
                            0 AS EsAveria,
                            {estadoSql} AS EstadoReporte

                        FROM dbo.TRAMITE t
                        {joinTipo}

                        UNION ALL

                        SELECT
                            N'Averia',
                            a.IdAveria,
                            N'Avería / fuga',

                            COALESCE(
                                (
                                    SELECT TOP 1 x.Estado
                                    FROM dbo.MPA_ORDEN_TRABAJO x
                                    WHERE x.IdAveria = a.IdAveria
                                    ORDER BY x.IdOrden DESC
                                ),
                                N'Recibido'
                            ),

                            a.FechaReporte,
                            1,
                            a.Estado

                        FROM dbo.AVERIA a
                    ) q

                    LEFT JOIN dbo.MPA_TRAMITE_ORIGEN o
                        ON o.TipoOrigen = q.TipoRegistro
                       AND o.IdRegistroOrigen = q.IdRegistro

                    WHERE
                        (
                            @Tipo IS NULL
                            OR q.Tipo LIKE N'%' + @Tipo + N'%'
                        )
                        AND
                        (
                            @Estado IS NULL
                            OR q.Estado = @Estado
                        )
                        AND
                        (
                            @Solo = 0
                            OR q.Estado IN
                            (
                                N'Recibido',
                                N'Pendiente',
                                N'En proceso',
                                N'En revisión'
                            )
                        )

                    ORDER BY
                        q.EsAveria DESC,
                        q.FechaRecepcion DESC;";

                ViewBag.Tramites = await Consultar(
                    consulta,

                    P(
                        "@Tipo",
                        string.IsNullOrWhiteSpace(tipo)
                            ? null : tipo.Trim()
                    ),

                    P(
                        "@Estado",
                        string.IsNullOrWhiteSpace(estado)
                            ? null : estado.Trim()
                    ),

                    P("@Solo", pendientes ? 1 : 0)
                );
            }
        }

        // MPA-05: AVERÍAS

        else if (tab == "averias")
        {
            ViewBag.Fontaneros = await Consultar(@"
                SELECT
                    IdFontanero,
                    Nombre,
                    Telefono,
                    Activo
                FROM dbo.MPA_FONTANERO
                ORDER BY Nombre;");

            ViewBag.Averias = await Consultar(@"
                SELECT
                    a.IdAveria,
                    a.Descripcion,
                    a.Prioridad,
                    a.FechaReporte,
                    a.Estado AS EstadoReporte,

                    o.IdOrden,
                    o.IdFontanero,
                    o.Estado AS EstadoOrden,
                    o.Observaciones,

                    f.Nombre AS Fontanero

                FROM dbo.AVERIA a

                OUTER APPLY
                (
                    SELECT TOP 1
                        IdOrden,
                        IdFontanero,
                        Estado,
                        Observaciones

                    FROM dbo.MPA_ORDEN_TRABAJO
                    WHERE IdAveria = a.IdAveria

                    ORDER BY
                        CASE
                            WHEN Estado = 'Cerrada'
                            THEN 1 ELSE 0
                        END,
                        IdOrden DESC

                ) o

                LEFT JOIN dbo.MPA_FONTANERO f
                    ON f.IdFontanero = o.IdFontanero

                ORDER BY a.FechaReporte DESC;");

            ViewBag.Ordenes = await Consultar(@"
                SELECT
                    o.IdOrden,
                    o.IdAveria,
                    f.Nombre AS Fontanero,
                    o.Estado,
                    o.SeResolvio,
                    o.Observaciones

                FROM dbo.MPA_ORDEN_TRABAJO o

                LEFT JOIN dbo.MPA_FONTANERO f
                    ON f.IdFontanero = o.IdFontanero

                ORDER BY
                    CASE
                        WHEN o.Estado = 'Cerrada'
                        THEN 1 ELSE 0
                    END,
                    o.IdOrden DESC;");
        }

        // MPA-06: ABONADOS

        else if (tab == "abonados")
        {
            ViewBag.Abonados = await Consultar(@"
                SELECT
                    a.IdAbonado,
                    a.NIS,
                    a.NumeroPaja,
                    a.Direccion,
                    a.NumeroFinca,
                    a.NumeroPlano,
                    a.IdTarifa,

                    u.Nombres,
                    u.Telefono,
                    u.CorreoElectronico,

                    CASE
                        WHEN EXISTS
                        (
                            SELECT 1
                            FROM dbo.RECIBO r

                            WHERE r.IdAbonado = a.IdAbonado

                              AND r.FechaVencimiento <
                                  DATEADD(day,-3,GETDATE())

                              AND r.Monto >
                                  COALESCE(
                                  (
                                      SELECT SUM(p.Monto)
                                      FROM dbo.PAGO p
                                      WHERE p.IdRecibo = r.IdRecibo
                                  ),0)
                        )
                        THEN 1
                        ELSE 0
                    END AS PosibleMora

                FROM dbo.ABONADO a

                LEFT JOIN dbo.USUARIO u
                    ON u.IdUsuario = a.IdUsuario

                ORDER BY a.NIS;");

            ViewBag.Tarifas = await Consultar(@"
                SELECT IdTarifa,Nombre
                FROM dbo.TARIFA
                WHERE Estado = 1
                ORDER BY Nombre;");

            if (idAbonado.HasValue)
            {
                ViewBag.AbonadoSeleccionado = await Consultar(@"
                    SELECT
                        a.IdAbonado,
                        a.NIS,
                        a.IdTarifa,
                        a.NumeroPaja,
                        a.NumeroFinca,
                        a.NumeroPlano,
                        a.Direccion,

                        u.Nombres,
                        u.Telefono,
                        u.CorreoElectronico

                    FROM dbo.ABONADO a

                    JOIN dbo.USUARIO u
                        ON u.IdUsuario = a.IdUsuario

                    WHERE a.IdAbonado = @Id;",

                    P("@Id", idAbonado.Value));

                ViewBag.Cambios = await Consultar(@"
                    SELECT
                        CampoModificado,
                        FechaModificacion,
                        ValorAnterior,
                        ValorNuevo

                    FROM dbo.MPA_ABONADO_CAMBIO

                    WHERE IdAbonado = @Id

                    ORDER BY FechaModificacion DESC;",

                    P("@Id", idAbonado.Value));

                ViewBag.Contratos = await Consultar(@"
                    SELECT
                        IdContrato,
                        NumeroVersion,
                        FechaGeneracion,
                        VersionTextoReglamento

                    FROM dbo.MPA_ABONADO_CONTRATO

                    WHERE IdAbonado = @Id

                    ORDER BY NumeroVersion DESC;",

                    P("@Id", idAbonado.Value));
            }
        }

        return View();
    }

    // =====================================================
    // MPA-02: GUARDAR Y EDITAR NOTICIAS
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarNoticia(
        int idNoticia,
        string? titulo,
        string? contenido,
        DateTime? fechaVigencia,
        string accion,
        IFormFile? imagen)
    {
        bool publicar = accion == "Publicar";

        if (
            string.IsNullOrWhiteSpace(titulo) ||
            titulo.Length > 200 ||
            string.IsNullOrWhiteSpace(contenido) ||
            (
                publicar &&
                (
                    !fechaVigencia.HasValue ||
                    fechaVigencia.Value.Date < DateTime.Today
                )
            )
        )
        {
            TempData["Error"] =
                "Completa título, contenido y vigencia válida.";

            return Volver("noticias");
        }

        string? ruta = null;

        if (imagen is { Length: > 0 })
        {
            if (imagen.Length > MaxImagen || !EsImagen(imagen))
            {
                TempData["Error"] =
                    "La imagen debe ser JPG, PNG o WEBP (hasta 5 MB).";

                return Volver("noticias");
            }

            var carpeta = Path.Combine(
                _env.WebRootPath,
                "img",
                "mpa-noticias");

            var archivo = await GuardarArchivo(
                imagen,
                carpeta,
                Path.GetExtension(imagen.FileName).ToLowerInvariant());

            ruta = "/img/mpa-noticias/" + Path.GetFileName(archivo);
        }

        if (publicar && idNoticia == 0 && ruta == null)
        {
            TempData["Error"] =
                "Adjunta una imagen para publicar.";

            return Volver("noticias");
        }

        if (idNoticia > 0)
        {
            var actualizados = await Ejecutar(@"
                UPDATE dbo.MPA_NOTICIA

                SET
                    Titulo = @Titulo,
                    Contenido = @Contenido,
                    FechaVigencia = @Vigencia,

                    RutaImagen =
                        COALESCE(@Imagen,RutaImagen),

                    Estado = @Estado,

                    FechaPublicacion =
                        CASE
                            WHEN @Estado = 'Publicado'
                            THEN COALESCE(
                                FechaPublicacion,
                                SYSDATETIME()
                            )
                            ELSE FechaPublicacion
                        END

                WHERE IdNoticia = @Id
                  AND
                  (
                      @Estado = 'Borrador'
                      OR COALESCE(
                          @Imagen,
                          RutaImagen
                      ) IS NOT NULL
                  );",

                P("@Id", idNoticia),
                P("@Titulo", titulo.Trim()),
                P("@Contenido", contenido.Trim()),
                P("@Vigencia", fechaVigencia),
                P("@Imagen", ruta),
                P(
                    "@Estado",
                    publicar ? "Publicado" : "Borrador"
                ));

            TempData[actualizados == 1 ? "Ok" : "Error"] =
                actualizados == 1
                    ? "Noticia actualizada."
                    : "No se pudo actualizar: falta una imagen.";
        }
        else
        {
            await Ejecutar(@"
                INSERT dbo.MPA_NOTICIA
                (
                    Titulo,
                    Contenido,
                    RutaImagen,
                    Estado,
                    FechaVigencia,
                    FechaPublicacion
                )
                VALUES
                (
                    @Titulo,
                    @Contenido,
                    @Imagen,
                    @Estado,
                    @Vigencia,
                    CASE
                        WHEN @Estado = 'Publicado'
                        THEN SYSDATETIME()
                        ELSE NULL
                    END
                );",

                P("@Titulo", titulo.Trim()),
                P("@Contenido", contenido.Trim()),
                P("@Imagen", ruta),
                P(
                    "@Estado",
                    publicar ? "Publicado" : "Borrador"
                ),
                P("@Vigencia", fechaVigencia));

            TempData["Ok"] = publicar
                ? "Noticia publicada."
                : "Borrador guardado.";
        }

        return Volver("noticias");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstadoNoticia(
        int idNoticia,
        string estado)
    {
        if (estado is not ("Borrador" or "Publicado"))
        {
            return BadRequest();
        }

        var filas = await Ejecutar(@"
            UPDATE dbo.MPA_NOTICIA

            SET
                Estado = @Estado,

                FechaPublicacion =
                    CASE
                        WHEN @Estado = 'Publicado'
                        THEN COALESCE(
                            FechaPublicacion,
                            SYSDATETIME()
                        )
                        ELSE FechaPublicacion
                    END

            WHERE IdNoticia = @Id

              AND
              (
                  @Estado = 'Borrador'

                  OR
                  (
                      RutaImagen IS NOT NULL

                      AND FechaVigencia >=
                          CONVERT(date,GETDATE())

                      AND NULLIF(
                          LTRIM(RTRIM(Titulo)),
                          ''
                      ) IS NOT NULL

                      AND NULLIF(
                          LTRIM(RTRIM(Contenido)),
                          ''
                      ) IS NOT NULL
                  )
              );",

            P("@Estado", estado),
            P("@Id", idNoticia));

        TempData[filas == 1 ? "Ok" : "Error"] =
            filas == 1
                ? (
                    estado == "Borrador"
                        ? "Noticia retirada."
                        : "Noticia publicada."
                  )
                : "Para publicar necesitas una imagen, contenido y vigencia válida.";

        return Volver("noticias");
    }

    private static bool EsImagen(IFormFile archivo)
    {
        var ext = Path.GetExtension(archivo.FileName)
            .ToLowerInvariant();

        if (ext is not (
            ".jpg" or ".jpeg" or ".png" or ".webp"))
        {
            return false;
        }

        using var stream = archivo.OpenReadStream();

        Span<byte> datos = stackalloc byte[12];

        if (stream.Read(datos) < 12)
        {
            return false;
        }

        return ext switch
        {
            ".jpg" or ".jpeg" =>
                datos[0] == 0xFF &&
                datos[1] == 0xD8,

            ".png" =>
                datos[0] == 0x89 &&
                datos[1] == 0x50 &&
                datos[2] == 0x4E,

            ".webp" =>
                Encoding.ASCII.GetString(datos[..4]) == "RIFF"
                &&
                Encoding.ASCII.GetString(datos[8..]) == "WEBP",

            _ => false
        };
    }

    // =====================================================
    // MPA-03: DOCUMENTOS Y VERSIONES
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarDocumento(
        int idDocumentoControl,
        string titulo,
        string tipoDocumento,
        string? descripcion)
    {
        if (
            idDocumentoControl < 1 ||
            string.IsNullOrWhiteSpace(titulo) ||
            titulo.Length > 250 ||
            descripcion?.Length > 1000 ||
            tipoDocumento is not (
                "Ficha institucional" or
                "Resolución exoneración"
            )
        )
        {
            return BadRequest();
        }

        var filas = await Ejecutar(@"
            UPDATE d

            SET
                Titulo = @Titulo,
                Descripcion = @Descripcion,
                TipoDocumento = @Tipo

            FROM dbo.MPA_DOCUMENTO_CONTROL d

            WHERE d.IdDocumentoControl = @Id

              AND
              (
                  d.TipoDocumento = @Tipo

                  OR NOT EXISTS
                  (
                      SELECT 1
                      FROM dbo.MPA_DOCUMENTO_VERSION v
                      WHERE v.IdDocumentoControl =
                            d.IdDocumentoControl
                  )
              );",

            P("@Id", idDocumentoControl),
            P("@Titulo", titulo.Trim()),
            P("@Descripcion", descripcion?.Trim()),
            P("@Tipo", tipoDocumento));

        TempData[filas == 1 ? "Ok" : "Error"] =
            filas == 1
                ? "Documento actualizado. Las versiones se conservaron."
                : "No puedes cambiar el tipo de un documento con versiones previas.";

        return Volver("documentos");
    }

    private static bool EsPdf(IFormFile? archivo)
    {
        if (
            archivo == null ||
            archivo.Length < 5 ||
            archivo.Length > MaxPdf ||
            !Path.GetExtension(archivo.FileName)
                .Equals(
                    ".pdf",
                    StringComparison.OrdinalIgnoreCase
                )
        )
        {
            return false;
        }

        using var stream = archivo.OpenReadStream();

        Span<byte> cabecera = stackalloc byte[5];

        return stream.Read(cabecera) == 5 &&
            Encoding.ASCII.GetString(cabecera) == "%PDF-";
    }

    private static async Task<string> GuardarArchivo(
        IFormFile archivo,
        string carpeta,
        string extension)
    {
        Directory.CreateDirectory(carpeta);

        var destino = Path.Combine(
            carpeta,
            Guid.NewGuid().ToString("N") + extension);

        await using var salida =
            System.IO.File.Create(destino);

        await archivo.CopyToAsync(salida);

        return destino;
    }

    private string CarpetaPrivada() =>
        Path.Combine(
            _env.ContentRootPath,
            "MPA_ArchivosPrivados");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NuevaVersion(
        int idDocumentoControl,
        string? titulo,
        string tipoDocumento,
        string? responsable,
        string? cargo,
        IFormFile? archivo,
        IFormFile? declaracionFirmada)
    {
        if (
            !EsPdf(archivo) ||
            string.IsNullOrWhiteSpace(responsable) ||
            string.IsNullOrWhiteSpace(cargo) ||
            (
                idDocumentoControl == 0 &&
                string.IsNullOrWhiteSpace(titulo)
            )
        )
        {
            TempData["Error"] =
                "Adjunta el PDF e indica responsable y cargo.";

            return Volver("documentos");
        }

        if (
            idDocumentoControl == 0 &&
            tipoDocumento is not (
                "Ficha institucional" or
                "Resolución exoneración"
            )
        )
        {
            return BadRequest();
        }

        bool esFicha =
            tipoDocumento == "Ficha institucional";

        if (idDocumentoControl > 0)
        {
            var tipo = await Consultar(@"
                SELECT TipoDocumento

                FROM dbo.MPA_DOCUMENTO_CONTROL

                WHERE IdDocumentoControl = @Id;",

                P("@Id", idDocumentoControl));

            if (tipo.Rows.Count == 0)
            {
                return NotFound();
            }

            esFicha =
                tipo.Rows[0][0].ToString() ==
                "Ficha institucional";
        }

        if (esFicha && !EsPdf(declaracionFirmada))
        {
            TempData["Error"] =
                "La ficha requiere adjuntar una declaración firmada en PDF.";

            return Volver("documentos");
        }

        var ruta = await GuardarArchivo(
            archivo!,
            CarpetaPrivada(),
            ".pdf");

        string? declaracion = null;

        if (
            declaracionFirmada is { Length: > 0 } &&
            EsPdf(declaracionFirmada)
        )
        {
            declaracion = await GuardarArchivo(
                declaracionFirmada,
                CarpetaPrivada(),
                ".pdf");
        }

        try
        {
            await using var cn = Conexion();
            await cn.OpenAsync();

            await using var tran =
                (SqlTransaction)await cn.BeginTransactionAsync();

            if (idDocumentoControl == 0)
            {
                await using var crear = new SqlCommand(@"
                    INSERT dbo.MPA_DOCUMENTO_CONTROL
                    (
                        Titulo,
                        TipoDocumento
                    )

                    OUTPUT INSERTED.IdDocumentoControl

                    VALUES (@T,@Tipo);",

                    cn,
                    tran);

                crear.Parameters.AddRange(new[]
                {
                    P("@T", titulo!.Trim()),
                    P("@Tipo", tipoDocumento)
                });

                idDocumentoControl = Convert.ToInt32(
                    await crear.ExecuteScalarAsync());
            }

            await using var cmd = new SqlCommand(@"
                DECLARE @Numero INT;

                SELECT @Numero =
                    ISNULL(MAX(NumeroVersion),0) + 1

                FROM dbo.MPA_DOCUMENTO_VERSION
                    WITH (UPDLOCK,HOLDLOCK)

                WHERE IdDocumentoControl = @Id;

                INSERT dbo.MPA_DOCUMENTO_VERSION
                (
                    IdDocumentoControl,
                    NumeroVersion,
                    RutaArchivo,
                    NombreArchivo,
                    Responsable,
                    Cargo,
                    RutaDeclaracionFirmada,
                    FirmaValidada
                )

                VALUES
                (
                    @Id,
                    @Numero,
                    @Ruta,
                    @Nombre,
                    @Responsable,
                    @Cargo,
                    @Declaracion,
                    0
                );",

                cn,
                tran);

            cmd.Parameters.AddRange(new[]
            {
                P("@Id", idDocumentoControl),
                P("@Ruta", ruta),

                P(
                    "@Nombre",
                    Path.GetFileName(archivo!.FileName)
                ),

                P("@Responsable", responsable!.Trim()),
                P("@Cargo", cargo!.Trim()),
                P("@Declaracion", declaracion)
            });

            await cmd.ExecuteNonQueryAsync();
            await tran.CommitAsync();
        }
        catch
        {
            System.IO.File.Delete(ruta);

            if (declaracion != null)
            {
                System.IO.File.Delete(declaracion);
            }

            throw;
        }

        TempData["Ok"] =
            "Versión guardada correctamente.";

        return Volver("documentos");
    }

    private IActionResult DescargarPrivado(
        string? ruta,
        string nombre)
    {
        if (string.IsNullOrWhiteSpace(ruta))
        {
            return NotFound();
        }

        var carpeta = Path.GetFullPath(
            CarpetaPrivada()) +
            Path.DirectorySeparatorChar;

        var archivo = Path.GetFullPath(ruta);

        if (
            !archivo.StartsWith(
                carpeta,
                StringComparison.OrdinalIgnoreCase
            )
            ||
            !System.IO.File.Exists(archivo)
        )
        {
            return NotFound();
        }

        return PhysicalFile(
            archivo,
            "application/pdf",
            Path.GetFileName(nombre));
    }

    [HttpGet]
    public async Task<IActionResult> DescargarVersion(int id)
    {
        var tabla = await Consultar(@"
            SELECT
                RutaArchivo,
                NombreArchivo

            FROM dbo.MPA_DOCUMENTO_VERSION

            WHERE IdVersion = @Id;",

            P("@Id", id));

        if (tabla.Rows.Count == 0)
        {
            return NotFound();
        }

        return DescargarPrivado(
            tabla.Rows[0][0].ToString(),
            tabla.Rows[0][1].ToString() ?? "documento.pdf");
    }

    // =====================================================
    // MPA-04: ESTADO Y CANAL DE TRÁMITES
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarTramite(
        int idTramite,
        string estado,
        string canalOrigen)
    {
        if (
            idTramite < 1 ||
            estado is not (
                "Pendiente" or
                "En proceso" or
                "En revisión" or
                "Cerrada"
            )
            ||
            canalOrigen is not (
                "Web" or
                "Presencial" or
                "Teléfono" or
                "Correo"
            )
        )
        {
            return BadRequest();
        }

        var columnas = await ColumnasTramite();

        var fecha = Elegir(
            columnas,
            "FechaSolicitud",
            "FechaRegistro",
            "FechaCreacion",
            "FechaTramite",
            "Fecha");

        if (fecha == null || !columnas.Contains("Estado"))
        {
            TempData["Error"] =
                "No se encontró una fecha de recepción compatible.";

            return Volver("tramites");
        }

        await using var cn = Conexion();
        await cn.OpenAsync();

        await using var tran =
            (SqlTransaction)await cn.BeginTransactionAsync();

        try
        {
            await using var cmd = new SqlCommand($@"
                UPDATE dbo.TRAMITE

                SET Estado = @Estado

                WHERE IdTramite = @Id;

                IF @@ROWCOUNT = 0
                    THROW 51001,'Trámite no encontrado',1;

                UPDATE dbo.MPA_TRAMITE_ORIGEN

                SET CanalOrigen = @Canal

                WHERE TipoOrigen = 'Tramite'
                  AND IdRegistroOrigen = @Id;

                IF @@ROWCOUNT = 0
                    INSERT dbo.MPA_TRAMITE_ORIGEN
                    (
                        TipoOrigen,
                        IdRegistroOrigen,
                        CanalOrigen,
                        FechaRecepcion
                    )

                    SELECT
                        'Tramite',
                        @Id,
                        @Canal,
                        TRY_CONVERT(
                            datetime2(0),
                            {Col(fecha)}
                        )

                    FROM dbo.TRAMITE

                    WHERE IdTramite = @Id;",

                cn,
                tran);

            cmd.Parameters.AddRange(new[]
            {
                P("@Estado", estado),
                P("@Canal", canalOrigen),
                P("@Id", idTramite)
            });

            await cmd.ExecuteNonQueryAsync();
            await tran.CommitAsync();

            TempData["Ok"] =
                "Trámite actualizado correctamente.";
        }
        catch
        {
            await tran.RollbackAsync();
            throw;
        }

        return Volver("tramites");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Canal(
        string tipoOrigen,
        int idRegistroOrigen,
        string canalOrigen)
    {
        if (
            tipoOrigen is not ("Tramite" or "Averia") ||
            idRegistroOrigen < 1 ||
            canalOrigen is not (
                "Web" or
                "Presencial" or
                "Teléfono" or
                "Correo"
            )
        )
        {
            return BadRequest();
        }

        string consultaFecha;

        if (tipoOrigen == "Tramite")
        {
            var fecha = Elegir(
                await ColumnasTramite(),
                "FechaSolicitud",
                "FechaRegistro",
                "FechaCreacion",
                "FechaTramite",
                "Fecha");

            if (fecha == null)
            {
                return BadRequest();
            }

            consultaFecha =
                $"SELECT TRY_CONVERT(datetime2(0),{Col(fecha)}) " +
                "FROM dbo.TRAMITE WHERE IdTramite=@Id;";
        }
        else
        {
            consultaFecha =
                "SELECT FechaReporte " +
                "FROM dbo.AVERIA " +
                "WHERE IdAveria=@Id;";
        }

        var tabla = await Consultar(
            consultaFecha,
            P("@Id", idRegistroOrigen));

        if (
            tabla.Rows.Count == 0 ||
            tabla.Rows[0][0] == DBNull.Value
        )
        {
            return BadRequest();
        }

        await Ejecutar(@"
            UPDATE dbo.MPA_TRAMITE_ORIGEN

            SET CanalOrigen = @Canal

            WHERE TipoOrigen = @Tipo
              AND IdRegistroOrigen = @Id;

            IF @@ROWCOUNT = 0
                INSERT dbo.MPA_TRAMITE_ORIGEN
                (
                    TipoOrigen,
                    IdRegistroOrigen,
                    CanalOrigen,
                    FechaRecepcion
                )

                VALUES
                (
                    @Tipo,
                    @Id,
                    @Canal,
                    @Fecha
                );",

            P("@Canal", canalOrigen),
            P("@Tipo", tipoOrigen),
            P("@Id", idRegistroOrigen),
            P("@Fecha", tabla.Rows[0][0]));

        TempData["Ok"] =
            "Canal actualizado.";

        return Volver("tramites");
    }

    // =====================================================
    // MPA-05: FONTANEROS, ASIGNACIÓN Y SEGUIMIENTO
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NuevoFontanero(
        string nombre,
        string? telefono)
    {
        if (
            string.IsNullOrWhiteSpace(nombre) ||
            nombre.Length > 200 ||
            telefono?.Length > 30
        )
        {
            return BadRequest();
        }

        await Ejecutar(@"
            INSERT dbo.MPA_FONTANERO
            (
                Nombre,
                Telefono
            )

            VALUES (@N,@T);",

            P("@N", nombre.Trim()),
            P("@T", telefono?.Trim()));

        TempData["Ok"] =
            "Fontanero registrado.";

        return Volver("averias");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarFontanero(
        int idFontanero,
        string nombre,
        string? telefono,
        bool activo)
    {
        if (
            idFontanero < 1 ||
            string.IsNullOrWhiteSpace(nombre) ||
            nombre.Length > 200 ||
            telefono?.Length > 30
        )
        {
            return BadRequest();
        }

        var filas = await Ejecutar(@"
            UPDATE dbo.MPA_FONTANERO

            SET
                Nombre = @N,
                Telefono = @T,
                Activo = @Activo

            WHERE IdFontanero = @Id;",

            P("@N", nombre.Trim()),
            P("@T", telefono?.Trim()),
            P("@Activo", activo),
            P("@Id", idFontanero));

        TempData[filas == 1 ? "Ok" : "Error"] =
            filas == 1
                ? "Fontanero actualizado."
                : "No se encontró el fontanero.";

        return Volver("averias");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Asignar(
        int idAveria,
        int idFontanero,
        string? observaciones)
    {
        var valido = await Consultar(@"
            SELECT CASE

                WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.AVERIA
                    WHERE IdAveria = @A
                )

                AND EXISTS
                (
                    SELECT 1
                    FROM dbo.MPA_FONTANERO
                    WHERE IdFontanero = @F
                      AND Activo = 1
                )

                THEN 1
                ELSE 0

            END;",

            P("@A", idAveria),
            P("@F", idFontanero));

        if (Convert.ToInt32(valido.Rows[0][0]) != 1)
        {
            return BadRequest();
        }

        await Ejecutar(@"
            DECLARE @Orden INT =
            (
                SELECT TOP 1 IdOrden

                FROM dbo.MPA_ORDEN_TRABAJO

                WHERE IdAveria = @A
                  AND Estado <> 'Cerrada'

                ORDER BY IdOrden DESC
            );

            IF @Orden IS NULL

                INSERT dbo.MPA_ORDEN_TRABAJO
                (
                    IdAveria,
                    IdFontanero,
                    Estado,
                    FechaAsignacion,
                    Observaciones
                )

                VALUES
                (
                    @A,
                    @F,
                    'En proceso',
                    SYSDATETIME(),
                    @O
                );

            ELSE

                UPDATE dbo.MPA_ORDEN_TRABAJO

                SET
                    IdFontanero = @F,
                    Estado = 'En proceso',
                    FechaAsignacion = SYSDATETIME(),
                    Observaciones = @O

                WHERE IdOrden = @Orden;",

            P("@A", idAveria),
            P("@F", idFontanero),
            P("@O", observaciones?.Trim()));

        TempData["Ok"] =
            "Orden asignada o reasignada.";

        return Volver("averias");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevisarOrden(
        int idOrden,
        bool seResolvio,
        string? observaciones)
    {
        var filas = await Ejecutar(@"
            UPDATE dbo.MPA_ORDEN_TRABAJO

            SET
                SeResolvio = @R,

                Estado =
                    CASE
                        WHEN @R = 1
                        THEN 'Cerrada'
                        ELSE 'En proceso'
                    END,

                FechaCierre =
                    CASE
                        WHEN @R = 1
                        THEN SYSDATETIME()
                        ELSE NULL
                    END,

                Observaciones = @O

            WHERE IdOrden = @Id
              AND Estado <> 'Cerrada';",

            P("@Id", idOrden),
            P("@R", seResolvio),
            P("@O", observaciones?.Trim()));

        TempData[filas == 1 ? "Ok" : "Error"] =
            filas == 0
                ? "La orden no existe o ya está cerrada."
                : (
                    seResolvio
                        ? "Orden cerrada."
                        : "Orden abierta para seguimiento."
                  );

        return Volver("averias");
    }

    // =====================================================
    // MPA-06: ABONADOS, HISTORIAL Y CONTRATOS
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Morosos()
    {
        var tabla = await Consultar(@"
            SELECT DISTINCT r.IdAbonado

            FROM dbo.RECIBO r

            WHERE r.FechaVencimiento <
                DATEADD(day,-3,GETDATE())

              AND r.Monto >
                COALESCE(
                (
                    SELECT SUM(p.Monto)
                    FROM dbo.PAGO p
                    WHERE p.IdRecibo = r.IdRecibo
                ),0);");

        return Json(
            tabla.Rows.Cast<DataRow>()
                .Select(f => Convert.ToInt32(f[0]))
                .ToArray());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarAbonado(
        int idAbonado,
        string? nombres,
        string? telefono,
        string? correoElectronico,
        string? direccion,
        string? numeroFinca,
        string? numeroPlano,
        int idTarifa)
    {
        nombres = nombres?.Trim();
        telefono = telefono?.Trim();
        correoElectronico = correoElectronico?.Trim();
        direccion = direccion?.Trim();
        numeroFinca = numeroFinca?.Trim();
        numeroPlano = numeroPlano?.Trim();

        if (
            idAbonado < 1 ||
            string.IsNullOrWhiteSpace(nombres) ||
            nombres.Length > 100 ||
            string.IsNullOrWhiteSpace(direccion) ||
            string.IsNullOrWhiteSpace(correoElectronico) ||
            !System.Net.Mail.MailAddress.TryCreate(
                correoElectronico,
                out _
            ) ||
            idTarifa < 1
        )
        {
            TempData["Error"] =
                "Revisa nombre, dirección, correo y tarifa.";

            return Volver("abonados", idAbonado);
        }

        await using var cn = Conexion();
        await cn.OpenAsync();

        await using var tran =
            (SqlTransaction)await cn.BeginTransactionAsync();

        var anteriores =
            new Dictionary<string, string?>();

        int idUsuario;
        string? idNetUser;

        await using (var cmd = new SqlCommand(@"
            SELECT
                a.IdUsuario,
                u.IdNetUser,
                u.Nombres,
                u.Telefono,
                u.CorreoElectronico,

                a.Direccion,
                a.NumeroFinca,
                a.NumeroPlano,

                CONVERT(varchar(20),a.IdTarifa)

            FROM dbo.ABONADO a WITH (UPDLOCK)

            JOIN dbo.USUARIO u WITH (UPDLOCK)
                ON u.IdUsuario = a.IdUsuario

            WHERE a.IdAbonado = @Id;",

            cn,
            tran))
        {
            cmd.Parameters.Add(P("@Id", idAbonado));

            await using var lector =
                await cmd.ExecuteReaderAsync();

            if (!await lector.ReadAsync())
            {
                return NotFound();
            }

            idUsuario = lector.GetInt32(0);

            idNetUser = lector.IsDBNull(1)
                ? null
                : lector.GetString(1);

            var campos = new[]
            {
                "Nombres",
                "Telefono",
                "CorreoElectronico",
                "Direccion",
                "NumeroFinca",
                "NumeroPlano",
                "IdTarifa"
            };

            for (int i = 0; i < campos.Length; i++)
            {
                anteriores[campos[i]] = lector.IsDBNull(i + 2)
                    ? null
                    : lector.GetValue(i + 2).ToString();
            }
        }

        var nuevos = new Dictionary<string, string?>
        {
            ["Nombres"] = nombres,
            ["Telefono"] = telefono,
            ["CorreoElectronico"] = correoElectronico,
            ["Direccion"] = direccion,
            ["NumeroFinca"] = numeroFinca,
            ["NumeroPlano"] = numeroPlano,

            ["IdTarifa"] =
                idTarifa.ToString(CultureInfo.InvariantCulture)
        };

        await using (var cmd = new SqlCommand(@"
            SELECT COUNT(*)

            FROM dbo.USUARIO

            WHERE CorreoElectronico = @Correo
              AND IdUsuario <> @Id;",

            cn,
            tran))
        {
            cmd.Parameters.AddRange(new[]
            {
                P("@Correo", correoElectronico),
                P("@Id", idUsuario)
            });

            if (Convert.ToInt32(
                await cmd.ExecuteScalarAsync()) > 0)
            {
                TempData["Error"] =
                    "El correo ya está registrado.";

                return Volver("abonados", idAbonado);
            }
        }

        await using (var cmd = new SqlCommand(@"
            UPDATE dbo.USUARIO

            SET
                Nombres = @N,
                Telefono = @T,
                CorreoElectronico = @C,
                FechaDeModificacion = SYSDATETIME()

            WHERE IdUsuario = @U;

            UPDATE dbo.ABONADO

            SET
                Direccion = @D,
                NumeroFinca = @F,
                NumeroPlano = @P,
                IdTarifa = @Tarifa

            WHERE IdAbonado = @A;

            UPDATE dbo.AspNetUsers

            SET
                Email = @C,
                NormalizedEmail = UPPER(@C)

            WHERE Id = @NetUser;",

            cn,
            tran))
        {
            cmd.Parameters.AddRange(new[]
            {
                P("@N", nombres),
                P("@T", telefono),
                P("@C", correoElectronico),
                P("@U", idUsuario),

                P("@D", direccion),
                P("@F", numeroFinca),
                P("@P", numeroPlano),
                P("@Tarifa", idTarifa),

                P("@A", idAbonado),
                P("@NetUser", idNetUser)
            });

            await cmd.ExecuteNonQueryAsync();
        }

        foreach (var campo in nuevos.Keys)
        {
            if (string.Equals(
                anteriores[campo],
                nuevos[campo],
                StringComparison.Ordinal))
            {
                continue;
            }

            await using var cmd = new SqlCommand(@"
                INSERT dbo.MPA_ABONADO_CAMBIO
                (
                    IdAbonado,
                    CampoModificado,
                    ValorAnterior,
                    ValorNuevo
                )

                VALUES
                (
                    @A,
                    @Campo,
                    @Antes,
                    @Despues
                );",

                cn,
                tran);

            cmd.Parameters.AddRange(new[]
            {
                P("@A", idAbonado),
                P("@Campo", campo),
                P("@Antes", anteriores[campo]),
                P("@Despues", nuevos[campo])
            });

            await cmd.ExecuteNonQueryAsync();
        }

        await tran.CommitAsync();

        TempData["Ok"] =
            "Datos del abonado actualizados.";

        return Volver("abonados", idAbonado);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegenerarContrato(
        int idAbonado,
        string? textoReglamento,
        string? versionReglamento)
    {
        if (
            idAbonado < 1 ||
            string.IsNullOrWhiteSpace(textoReglamento) ||
            textoReglamento.Length > 900 ||
            string.IsNullOrWhiteSpace(versionReglamento) ||
            versionReglamento.Length > 100
        )
        {
            TempData["Error"] =
                "Indica el texto y versión del reglamento.";

            return Volver("abonados", idAbonado);
        }

        var datos = await Consultar(@"
            SELECT
                a.NIS,
                a.NumeroPaja,
                a.NumeroFinca,
                a.NumeroPlano,
                a.Direccion,

                t.Nombre AS Tarifa,
                u.Nombres

            FROM dbo.ABONADO a

            JOIN dbo.TARIFA t
                ON t.IdTarifa = a.IdTarifa

            JOIN dbo.USUARIO u
                ON u.IdUsuario = a.IdUsuario

            WHERE a.IdAbonado = @Id;",

            P("@Id", idAbonado));

        if (datos.Rows.Count == 0)
        {
            return NotFound();
        }

        var r = datos.Rows[0];

        var lineas = new[]
        {
            "ASADA SAN JOSE DE LA MONTANA - CONTRATO ACTUALIZADO",

            "Fecha de generacion: " +
                DateTime.Now.ToString("yyyy-MM-dd HH:mm"),

            "Abonado: " + r["Nombres"],
            "NIS: " + r["NIS"],
            "Numero de paja: " + r["NumeroPaja"],
            "Finca: " + r["NumeroFinca"],
            "Plano: " + r["NumeroPlano"],
            "Direccion: " + r["Direccion"],
            "Tarifa: " + r["Tarifa"],

            "",

            "TEXTO DE ACEPTACION DEL REGLAMENTO (SUMINISTRADO POR ASADA):",

            textoReglamento,

            "Version del reglamento: " + versionReglamento
        };

        var ruta = Path.Combine(
            CarpetaPrivada(),
            Guid.NewGuid().ToString("N") + ".pdf");

        Directory.CreateDirectory(CarpetaPrivada());

        await System.IO.File.WriteAllBytesAsync(
            ruta,
            CrearPdfBasico(lineas));

        try
        {
            await Ejecutar(@"
                DECLARE @Numero INT;

                SELECT @Numero =
                    ISNULL(MAX(NumeroVersion),0) + 1

                FROM dbo.MPA_ABONADO_CONTRATO
                    WITH (UPDLOCK,HOLDLOCK)

                WHERE IdAbonado = @Id;

                INSERT dbo.MPA_ABONADO_CONTRATO
                (
                    IdAbonado,
                    NumeroVersion,
                    RutaArchivoPDF,
                    VersionTextoReglamento
                )

                VALUES
                (
                    @Id,
                    @Numero,
                    @Ruta,
                    @Version
                );",

                P("@Id", idAbonado),
                P("@Ruta", ruta),
                P("@Version", versionReglamento.Trim()));
        }
        catch
        {
            System.IO.File.Delete(ruta);
            throw;
        }

        TempData["Ok"] =
            "Nueva versión del contrato generada.";

        return Volver("abonados", idAbonado);
    }

    [HttpGet]
    public async Task<IActionResult> DescargarContrato(int id)
    {
        var tabla = await Consultar(@"
            SELECT RutaArchivoPDF

            FROM dbo.MPA_ABONADO_CONTRATO

            WHERE IdContrato = @Id;",

            P("@Id", id));

        if (tabla.Rows.Count == 0)
        {
            return NotFound();
        }

        return DescargarPrivado(
            tabla.Rows[0][0].ToString(),
            "Contrato_ASADA.pdf");
    }

    // =====================================================
    // GENERACIÓN BÁSICA DEL PDF DE CONTRATO
    // =====================================================

    private static byte[] CrearPdfBasico(
        IEnumerable<string> lineas)
    {
        static string Convertir(string txt)
        {
            var normal = txt.Normalize(
                NormalizationForm.FormD);

            var sb = new StringBuilder();

            foreach (char c in normal)
            {
                if (
                    CharUnicodeInfo.GetUnicodeCategory(c) !=
                    UnicodeCategory.NonSpacingMark
                )
                {
                    sb.Append(c switch
                    {
                        '–' => '-',
                        '“' => '"',
                        '”' => '"',
                        '’' => '\'',
                        _ => c
                    });
                }
            }

            return new string(
                sb.ToString()
                    .Select(c => c <= 127 ? c : '?')
                    .ToArray());
        }

        static string Escapar(string s) =>
            Convertir(s)
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)");

        var txt = new StringBuilder(
            "BT /F1 10 Tf 40 790 Td 13 TL\n");

        int n = 0;

        foreach (var linea in lineas)
        {
            string frase = Convertir(linea ?? "");

            for (
                int i = 0;
                i < Math.Max(1, (frase.Length + 88) / 89);
                i++
            )
            {
                if (n++ > 48)
                {
                    break;
                }

                var trozo = frase.Substring(
                    Math.Min(i * 89, frase.Length),

                    Math.Min(
                        89,
                        Math.Max(0, frase.Length - i * 89)
                    )
                );

                txt.Append('(')
                   .Append(Escapar(trozo))
                   .Append(") Tj T*\n");
            }

            if (n > 48)
            {
                break;
            }
        }

        txt.Append("ET\n");

        string contenido = txt.ToString();

        string[] objetos =
        {
            "<< /Type /Catalog /Pages 2 0 R >>",

            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",

            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",

            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",

            $"<< /Length {Encoding.ASCII.GetByteCount(contenido)} >>\nstream\n{contenido}endstream"
        };

        var pdf = new StringBuilder("%PDF-1.4\n");

        var offsets = new List<int> { 0 };

        for (int i = 0; i < objetos.Length; i++)
        {
            offsets.Add(
                Encoding.ASCII.GetByteCount(pdf.ToString()));

            pdf.Append(
                $"{i + 1} 0 obj\n{objetos[i]}\nendobj\n");
        }

        int xref = Encoding.ASCII.GetByteCount(
            pdf.ToString());

        pdf.Append(
            $"xref\n0 {offsets.Count}\n0000000000 65535 f \n");

        for (int i = 1; i < offsets.Count; i++)
        {
            pdf.Append(
                $"{offsets[i]:D10} 00000 n \n");
        }

        pdf.Append(
            $"trailer << /Size {offsets.Count} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");

        return Encoding.ASCII.GetBytes(pdf.ToString());
    }
}
