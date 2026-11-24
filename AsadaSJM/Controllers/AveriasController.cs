using AsadaSJM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AsadaSJM.Controllers;

[Authorize(Roles = "Administrador,Operativo")]
public class AveriasController : Controller
{
    private static readonly string[] EstadosValidos =
        { "Pendiente", "En proceso", "Resuelta" };

    private readonly IConfiguration _configuration;

    public AveriasController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string ObtenerConnectionString()
    {
        string? cs = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(cs))
        {
            throw new InvalidOperationException(
                "No se encontró la conexión DefaultConnection en appsettings.json.");
        }

        return cs;
    }

    private int? ObtenerIdUsuarioAutenticado()
    {
        string? valor = User.FindFirst("IdUsuario")?.Value;
        return int.TryParse(valor, out int id) ? id : null;
    }

    // Lista de reportes de todos los abonados
    [HttpGet]
    public IActionResult Index(string? estado)
    {
        var modelo = new AveriasAdminViewModel
        {
            EstadoFiltro = string.IsNullOrWhiteSpace(estado) ? null : estado
        };

        using SqlConnection connection = new SqlConnection(ObtenerConnectionString());
        using SqlCommand command = new SqlCommand("SP_Averia_ListarAdmin", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add("@Estado", SqlDbType.VarChar, 50).Value =
            modelo.EstadoFiltro != null ? modelo.EstadoFiltro : DBNull.Value;

        connection.Open();
        using SqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            modelo.Averias.Add(AveriaLector.Leer(reader));
        }

        return View(modelo);
    }

    // Detalle del reporte + formulario para actualizar estado
    [HttpGet]
    public IActionResult Detalle(int id)
    {
        using SqlConnection connection = new SqlConnection(ObtenerConnectionString());
        using SqlCommand command = new SqlCommand("SP_Averia_DetalleAdmin", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add("@IdAveria", SqlDbType.Int).Value = id;

        connection.Open();
        using SqlDataReader reader = command.ExecuteReader();

        AveriaViewModel? modelo = AveriaLector.LeerDetalle(reader);

        if (modelo == null)
        {
            return NotFound();
        }

        return View(modelo);
    }

    // MRA-05: actualizar estado, fecha de atención y observaciones
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ActualizarEstado(ActualizarEstadoAveriaViewModel form)
    {
        int? idUsuario = ObtenerIdUsuarioAutenticado();

        if (idUsuario == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!EstadosValidos.Contains(form.Estado))
        {
            TempData["Error"] = "El estado seleccionado no es válido.";
            return RedirectToAction(nameof(Detalle), new { id = form.IdAveria });
        }

        if (string.IsNullOrWhiteSpace(form.Observaciones))
        {
            TempData["Error"] = "Debe indicar un comentario sobre la atención.";
            return RedirectToAction(nameof(Detalle), new { id = form.IdAveria });
        }

        try
        {
            using SqlConnection connection = new SqlConnection(ObtenerConnectionString());
            using SqlCommand command = new SqlCommand("SP_Averia_ActualizarEstado", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("@IdAveria", SqlDbType.Int).Value = form.IdAveria;
            command.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario.Value;
            command.Parameters.Add("@Estado", SqlDbType.VarChar, 50).Value = form.Estado;
            command.Parameters.Add("@FechaAtencion", SqlDbType.DateTime).Value =
                form.FechaAtencion.HasValue ? form.FechaAtencion.Value : DBNull.Value;
            command.Parameters.Add("@Observaciones", SqlDbType.VarChar, -1).Value =
                form.Observaciones.Trim();

            connection.Open();
            command.ExecuteNonQuery();
        }
        catch (SqlException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Detalle), new { id = form.IdAveria });
        }

        TempData["Exito"] = "El reporte fue actualizado correctamente.";
        return RedirectToAction(nameof(Detalle), new { id = form.IdAveria });
    }
}