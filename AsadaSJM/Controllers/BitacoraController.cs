using AsadaSJM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace AsadaSJM.Controllers;

[Authorize(Roles = "Administrador")]
public class BitacoraController : Controller
{
    private readonly IConfiguration _configuration;

    public BitacoraController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // GET: Bitacora
    public async Task<IActionResult> Index()
    {
        var registros = new List<BitacoraItemViewModel>();

        using var connection = new SqlConnection(
            _configuration.GetConnectionString("DefaultConnection"));

        using var command = new SqlCommand(@"
            SELECT
                B.IdBitacora,
                B.IdUsuario,
                B.Fecha,
                B.Accion,
                U.Nombres,
                U.PrimerApellido
            FROM BITACORA B
            LEFT JOIN USUARIO U ON U.IdUsuario = B.IdUsuario
            ORDER BY B.Fecha DESC", connection);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            string nombres = reader["Nombres"] as string ?? "";
            string apellido = reader["PrimerApellido"] as string ?? "";
            string nombreCompleto = $"{nombres} {apellido}".Trim();

            registros.Add(new BitacoraItemViewModel
            {
                IdBitacora = (int)reader["IdBitacora"],
                IdUsuario = reader["IdUsuario"] is DBNull ? 0 : (int)reader["IdUsuario"],
                Accion = reader["Accion"]?.ToString() ?? "",
                FechaHora = (DateTime)reader["Fecha"],
                Usuario = new Usuario
                {
                    Nombre = string.IsNullOrWhiteSpace(nombreCompleto)
                        ? "Usuario eliminado"
                        : nombreCompleto
                }
            });
        }

        return View(registros);
    }
}