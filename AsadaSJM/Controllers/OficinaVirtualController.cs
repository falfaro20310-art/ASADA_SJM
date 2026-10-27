
using AsadaSJM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

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
}

