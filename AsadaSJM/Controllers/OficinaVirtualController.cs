using AsadaSJM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace AsadaSJM.Controllers;

[Authorize(Roles = "Abonado")]
public class OficinaVirtualController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public OficinaVirtualController(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    // Métodos auxiliares 

    private int? ObtenerIdUsuarioAutenticado()
    {
        string? valor = User.FindFirst("IdUsuario")?.Value;

        if (!int.TryParse(valor, out int idUsuario))
        {
            return null;
        }

        return idUsuario;
    }

    private string ObtenerConnectionString()
    {
        string? connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "No se encontró la conexión DefaultConnection en appsettings.json.");
        }

        return connectionString;
    }

    // Inicio de la Oficina Virtual del abonado

    [HttpGet]
    public IActionResult Index()
    {
        int? idUsuario = ObtenerIdUsuarioAutenticado();

        if (idUsuario == null)
        {
            return RedirectToAction("Login", "Account");
        }

        string connectionString;

        try
        {
            connectionString = ObtenerConnectionString();
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, ex.Message);
        }

        using SqlConnection connection =
            new SqlConnection(connectionString);

        using SqlCommand command =
            new SqlCommand(
                "SP_ObtenerDatosOficinaVirtual",
                connection);

        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add("@IdUsuario", SqlDbType.Int)
            .Value = idUsuario.Value;

        connection.Open();

        using SqlDataReader reader = command.ExecuteReader();

        if (!reader.Read())
        {
            return View("SinAbonado");
        }

        string nombreCompleto =
            $"{reader["Nombres"]} " +
            $"{reader["PrimerApellido"]} " +
            $"{reader["SegundoApellido"]}";

        var modelo = new OficinaVirtualViewModel
        {
            IdAbonado = Convert.ToInt32(reader["IdAbonado"]),
            NombreCompleto = nombreCompleto.Trim(),
            Correo = reader["CorreoElectronico"]?.ToString() ?? "",
            Telefono = reader["Telefono"]?.ToString() ?? "",
            NIS = reader["NIS"]?.ToString() ?? "",
            NumeroPaja = reader["NumeroPaja"]?.ToString() ?? "",
            NumeroFinca = reader["NumeroFinca"]?.ToString() ?? "",
            NumeroPlano = reader["NumeroPlano"]?.ToString() ?? "",
            Direccion = reader["Direccion"]?.ToString() ?? "",
            Tarifa = reader["Tarifa"]?.ToString() ?? "",
            Sector = reader["Sector"]?.ToString() ?? ""
        };

        return View(modelo);
    }

    // Mis recibos del abonado 

    [HttpGet]
    public IActionResult MisRecibos()
    {
        int? idUsuario = ObtenerIdUsuarioAutenticado();

        if (idUsuario == null)
        {
            return RedirectToAction("Login", "Account");
        }

        string connectionString = ObtenerConnectionString();
        string nis;

        // Primero se verifica que el usuario tenga un abonado
        using (SqlConnection connection =
               new SqlConnection(connectionString))
        {
            using SqlCommand command = new SqlCommand(
                "SP_ObtenerDatosOficinaVirtual",
                connection);

            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("@IdUsuario", SqlDbType.Int)
                .Value = idUsuario.Value;

            connection.Open();

            using SqlDataReader reader = command.ExecuteReader();

            if (!reader.Read())
            {
                return View("SinAbonado");
            }

            nis = reader["NIS"]?.ToString() ?? "";
        }

        var recibos = new List<ReciboAbonadoViewModel>();

        // Se consultan los recibos del abonado autenticado
        using (SqlConnection connection =
               new SqlConnection(connectionString))
        {
            using SqlCommand command = new SqlCommand(
                "SP_ConsultarRecibosAbonado",
                connection);

            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("@IdUsuario", SqlDbType.Int)
                .Value = idUsuario.Value;

            command.Parameters.Add("@NIS", SqlDbType.VarChar, 30)
                .Value = nis;

            connection.Open();

            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                recibos.Add(new ReciboAbonadoViewModel
                {
                    IdRecibo = Convert.ToInt32(reader["IdRecibo"]),
                    NIS = reader["NIS"]?.ToString() ?? "",
                    NumeroPaja = reader["NumeroPaja"]?.ToString() ?? "",
                    Periodo = reader["Periodo"]?.ToString() ?? "",
                    Monto = Convert.ToDecimal(reader["Monto"]),
                    FechaEmision = Convert.ToDateTime(reader["FechaEmision"]),
                    FechaVencimiento = Convert.ToDateTime(reader["FechaVencimiento"]),
                    RutaPDF = reader["RutaPDF"] == DBNull.Value
                        ? null
                        : reader["RutaPDF"].ToString(),
                    Estado = reader["Estado"]?.ToString() ?? ""
                });
            }
        }

        return View(recibos);
    }

    // Recibos pendientes del abonado 

    [HttpGet]
    public IActionResult RecibosPendientes()
    {
        int? idUsuario = ObtenerIdUsuarioAutenticado();

        if (idUsuario == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var recibos = new List<ReciboPendienteViewModel>();

        using SqlConnection connection =
            new SqlConnection(ObtenerConnectionString());

        using SqlCommand command = new SqlCommand(
            "SP_ConsultarRecibosPendientes",
            connection);

        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add("@IdUsuario", SqlDbType.Int)
            .Value = idUsuario.Value;

        connection.Open();

        using SqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            recibos.Add(new ReciboPendienteViewModel
            {
                IdRecibo = Convert.ToInt32(reader["IdRecibo"]),
                NIS = reader["NIS"]?.ToString() ?? "",
                Periodo = reader["Periodo"]?.ToString() ?? "",
                Monto = Convert.ToDecimal(reader["Monto"]),
                FechaVencimiento =
                    Convert.ToDateTime(reader["FechaVencimiento"]),
                Estado = reader["Estado"]?.ToString() ?? ""
            });
        }

        return View(recibos);
    }

    // Mi consumo de agua con filtro de fechas 

    [HttpGet]
    public IActionResult MiConsumo(
        DateTime? fechaInicio,
        DateTime? fechaFin)
    {
        int? idUsuario = ObtenerIdUsuarioAutenticado();

        if (idUsuario == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var modelo = new FiltroConsumoViewModel
        {
            FechaInicio = fechaInicio,
            FechaFin = fechaFin
        };

        if (fechaInicio.HasValue &&
            fechaFin.HasValue &&
            fechaInicio.Value.Date > fechaFin.Value.Date)
        {
            ModelState.AddModelError(
                string.Empty,
                "La fecha inicial no puede ser posterior a la fecha final.");

            return View(modelo);
        }

        using SqlConnection connection =
            new SqlConnection(ObtenerConnectionString());

        using SqlCommand command = new SqlCommand(
            "SP_ConsultarConsumoAbonado",
            connection);

        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add("@IdUsuario", SqlDbType.Int)
            .Value = idUsuario.Value;

        // Se envían las fechas como NULL cuando no se eligió un filtro.
        command.Parameters.Add("@FechaInicio", SqlDbType.DateTime)
            .Value = fechaInicio.HasValue
                ? (object)fechaInicio.Value.Date
                : DBNull.Value;

        // Se incluye todo el día final.
        command.Parameters.Add("@FechaFin", SqlDbType.DateTime)
            .Value = fechaFin.HasValue
                ? (object)fechaFin.Value.Date.AddDays(1).AddMilliseconds(-3)
                : DBNull.Value;

        connection.Open();

        using SqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            modelo.Consumos.Add(new ConsumoAbonadoViewModel
            {
                IdConsumo = Convert.ToInt32(reader["IdConsumo"]),
                NIS = reader["NIS"]?.ToString() ?? "",
                Periodo = reader["Periodo"]?.ToString() ?? "",
                MetrosCubicos = Convert.ToDecimal(reader["MetrosCubicos"]),
                FechaLectura = Convert.ToDateTime(reader["FechaLectura"])
            });
        }

        return View(modelo);
    }

    // Para descargar el PDF del recibo, si está disponible.

    [HttpGet]
    public IActionResult DescargarRecibo(int id)
    {
        int? idUsuario = ObtenerIdUsuarioAutenticado();

        if (idUsuario == null)
        {
            return RedirectToAction("Login", "Account");
        }

        string? rutaPdf;

        using (SqlConnection connection =
               new SqlConnection(ObtenerConnectionString()))
        {
            using SqlCommand command = new SqlCommand(
                "SP_ObtenerReciboAbonado",
                connection);

            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("@IdUsuario", SqlDbType.Int)
                .Value = idUsuario.Value;

            command.Parameters.Add("@IdRecibo", SqlDbType.Int)
                .Value = id;

            connection.Open();

            using SqlDataReader reader = command.ExecuteReader();

            if (!reader.Read())
            {
                return NotFound("No se encontró el recibo solicitado.");
            }

            rutaPdf = reader["RutaPDF"] == DBNull.Value
                ? null
                : reader["RutaPDF"].ToString();
        }

        if (string.IsNullOrWhiteSpace(rutaPdf))
        {
            return NotFound("Este recibo no tiene un PDF disponible.");
        }

        // La ruta guardada debe ser relativa a wwwroot,
        // por ejemplo: recibos/recibo-123.pdf.
        string rutaRelativa = rutaPdf.Replace(
            '/',
            Path.DirectorySeparatorChar);

        if (Path.IsPathRooted(rutaRelativa) ||
            rutaRelativa.Contains(".."))
        {
            return BadRequest("La ruta del archivo no es válida.");
        }

        string webRootPath = _environment.WebRootPath
            ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        string rutaCompleta = Path.GetFullPath(
            Path.Combine(webRootPath, rutaRelativa));

        string raizWeb = Path.GetFullPath(webRootPath)
            .TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!rutaCompleta.StartsWith(
                raizWeb,
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("La ruta del archivo no es válida.");
        }

        if (!System.IO.File.Exists(rutaCompleta))
        {
            return NotFound("El archivo PDF no existe en el servidor.");
        }

        return PhysicalFile(
            rutaCompleta,
            "application/pdf",
            Path.GetFileName(rutaCompleta));
    }

    // =========================================================
    // REPORTE DE AVERÍAS (MRA-01 a MRA-06)
    // =========================================================

    private static readonly string[] ExtensionesImagen =
        { ".jpg", ".jpeg", ".png", ".webp" };

    private const int MaxFotos = 5;
    private const long MaxBytesFoto = 5 * 1024 * 1024;

    private List<CategoriaAveriaItem> ObtenerCategoriasAveria()
    {
        var categorias = new List<CategoriaAveriaItem>();

        using SqlConnection connection = new SqlConnection(ObtenerConnectionString());
        using SqlCommand command = new SqlCommand("SP_Averia_ObtenerCategorias", connection);
        command.CommandType = CommandType.StoredProcedure;

        connection.Open();
        using SqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            categorias.Add(new CategoriaAveriaItem
            {
                IdCategoriaAveria = Convert.ToInt32(reader["IdCategoriaAveria"]),
                Nombre = reader["Nombre"]?.ToString() ?? ""
            });
        }

        return categorias;
    }

    // Devuelve la dirección del abonado, o null si el usuario no tiene abonado.
    private string? ObtenerDireccionAbonado(int idUsuario)
    {
        using SqlConnection connection = new SqlConnection(ObtenerConnectionString());
        using SqlCommand command = new SqlCommand("SP_ObtenerDatosOficinaVirtual", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;

        connection.Open();
        using SqlDataReader reader = command.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }

        return reader["Direccion"]?.ToString() ?? "";
    }

    // MRA-06: mis reportes + búsqueda por número de caso
    [HttpGet]
    public IActionResult MisAverias(string? numeroCaso)
    {
        int? idUsuario = ObtenerIdUsuarioAutenticado();

        if (idUsuario == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (ObtenerDireccionAbonado(idUsuario.Value) == null)
        {
            return View("SinAbonado");
        }

        var modelo = new MisAveriasViewModel { NumeroCasoBuscado = numeroCaso };
        int? idAveria = null;

        if (!string.IsNullOrWhiteSpace(numeroCaso))
        {
            string digitos = new string(numeroCaso.Where(char.IsDigit).ToArray());

            if (int.TryParse(digitos, out int id))
            {
                idAveria = id;
            }
            else
            {
                ModelState.AddModelError(string.Empty,
                    "El número de caso no es válido. Ejemplo: AV-000123.");
            }
        }

        using SqlConnection connection = new SqlConnection(ObtenerConnectionString());
        using SqlCommand command = new SqlCommand("SP_Averia_ListarPorAbonado", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario.Value;
        command.Parameters.Add("@IdAveria", SqlDbType.Int).Value =
            idAveria.HasValue ? idAveria.Value : DBNull.Value;

        connection.Open();
        using SqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            modelo.Averias.Add(AveriaLector.Leer(reader));
        }

        return View(modelo);
    }

    // MRA-01 / MRA-02 / MRA-03: formulario de reporte
    [HttpGet]
    public IActionResult ReportarAveria()
    {
        int? idUsuario = ObtenerIdUsuarioAutenticado();

        if (idUsuario == null)
        {
            return RedirectToAction("Login", "Account");
        }

        string? direccion = ObtenerDireccionAbonado(idUsuario.Value);

        if (direccion == null)
        {
            return View("SinAbonado");
        }

        var modelo = new ReportarAveriaViewModel
        {
            Ubicacion = direccion,
            Categorias = ObtenerCategoriasAveria()
        };

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ReportarAveria(ReportarAveriaViewModel modelo)
    {
        int? idUsuario = ObtenerIdUsuarioAutenticado();

        if (idUsuario == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var fotos = modelo.Fotografias
            .Where(foto => foto != null && foto.Length > 0)
            .ToList();

        if (fotos.Count > MaxFotos)
        {
            ModelState.AddModelError("Fotografias",
                $"Puede adjuntar un máximo de {MaxFotos} fotografías.");
        }

        foreach (var foto in fotos)
        {
            string extension = Path.GetExtension(foto.FileName).ToLowerInvariant();

            if (!ExtensionesImagen.Contains(extension))
            {
                ModelState.AddModelError("Fotografias",
                    $"\"{foto.FileName}\" no es una imagen válida (JPG, PNG o WEBP).");
            }
            else if (foto.Length > MaxBytesFoto)
            {
                ModelState.AddModelError("Fotografias",
                    $"\"{foto.FileName}\" supera el tamaño máximo de 5 MB.");
            }
        }

        if (!ModelState.IsValid)
        {
            modelo.Categorias = ObtenerCategoriasAveria();
            return View(modelo);
        }

        int idAveria;

        try
        {
            using SqlConnection connection = new SqlConnection(ObtenerConnectionString());
            using SqlCommand command = new SqlCommand("SP_Averia_Registrar", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario.Value;
            command.Parameters.Add("@IdCategoriaAveria", SqlDbType.Int).Value =
                modelo.IdCategoriaAveria;
            command.Parameters.Add("@Descripcion", SqlDbType.VarChar, -1).Value =
                modelo.Descripcion.Trim();
            command.Parameters.Add("@Ubicacion", SqlDbType.VarChar, 500).Value =
                modelo.Ubicacion.Trim();

            connection.Open();
            idAveria = Convert.ToInt32(command.ExecuteScalar());
        }
        catch (SqlException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            modelo.Categorias = ObtenerCategoriasAveria();
            return View(modelo);
        }

        // MRA-02: guardar las fotografías en wwwroot/uploads/averias
        if (fotos.Count > 0)
        {
            string webRoot = _environment.WebRootPath
                ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            string carpeta = Path.Combine(webRoot, "uploads", "averias");
            Directory.CreateDirectory(carpeta);

            foreach (var foto in fotos)
            {
                string extension = Path.GetExtension(foto.FileName).ToLowerInvariant();
                string nombreArchivo = $"{Guid.NewGuid():N}{extension}";
                string rutaCompleta = Path.Combine(carpeta, nombreArchivo);

                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    foto.CopyTo(stream);
                }

                using SqlConnection connection = new SqlConnection(ObtenerConnectionString());
                using SqlCommand command = new SqlCommand("SP_Averia_AgregarImagen", connection);
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario.Value;
                command.Parameters.Add("@IdAveria", SqlDbType.Int).Value = idAveria;
                command.Parameters.Add("@RutaImagen", SqlDbType.VarChar, 500).Value =
                    "uploads/averias/" + nombreArchivo;

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        // MRA-04: número de caso
        TempData["AveriaCreada"] = $"AV-{idAveria:000000}";

        return RedirectToAction(nameof(DetalleAveria), new { id = idAveria });
    }

    // Detalle de un reporte propio
    [HttpGet]
    public IActionResult DetalleAveria(int id)
    {
        int? idUsuario = ObtenerIdUsuarioAutenticado();

        if (idUsuario == null)
        {
            return RedirectToAction("Login", "Account");
        }

        using SqlConnection connection = new SqlConnection(ObtenerConnectionString());
        using SqlCommand command = new SqlCommand("SP_Averia_DetalleAbonado", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario.Value;
        command.Parameters.Add("@IdAveria", SqlDbType.Int).Value = id;

        connection.Open();
        using SqlDataReader reader = command.ExecuteReader();

        AveriaViewModel? modelo = AveriaLector.LeerDetalle(reader);

        if (modelo == null)
        {
            return NotFound("No se encontró el reporte solicitado.");
        }

        return View(modelo);
    }
}

