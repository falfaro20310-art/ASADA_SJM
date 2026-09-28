using AsadaSJM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AsadaSJM.Controllers;

[Authorize(Roles = "Abonado")]
public class OficinaVirtualController : Controller
{
    private readonly IConfiguration _configuration;

    public OficinaVirtualController(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }


    // Inicio de la oficina vitual, se obtiene la información del abonado y se muestra la vista 

    [HttpGet]
    public IActionResult Index()
    {
        
        // 1. Para obtener IdUsuario desde la cookie

        string? idUsuarioClaim =
            User.FindFirst("IdUsuario")?.Value;

        if (string.IsNullOrWhiteSpace(idUsuarioClaim))
        {
            return RedirectToAction(
                "Login",
                "Account");
        }


        if (!int.TryParse(
                idUsuarioClaim,
                out int idUsuario))
        {
            return RedirectToAction(
                "Login",
                "Account");
        }


        // 2. Obtener la conexión con la bd desde el appsettings.json

        string? connectionString =
            _configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return StatusCode(
                500,
                "No se encontró la conexión a la base de datos.");
        }


        // 3. Ejecutar Store Procedure para obtener los datos del abonado

        using SqlConnection connection =
            new SqlConnection(
                connectionString);

        using SqlCommand command =
            new SqlCommand(
                "SP_ObtenerDatosOficinaVirtual",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.AddWithValue(
            "@IdUsuario",
            idUsuario);

        connection.Open();


        using SqlDataReader reader =
            command.ExecuteReader();


        // 4. Se verifica el abonado

        if (!reader.Read())
        {
            return View(
                "SinAbonado");
        }


        // 5. Se crea el modelo de la vista con los datos obtenidos del SP

        string nombreCompleto =
            $"{reader["Nombres"]} " +
            $"{reader["PrimerApellido"]} " +
            $"{reader["SegundoApellido"]}";


        var modelo =
            new OficinaVirtualViewModel
            {
                IdAbonado =
                    Convert.ToInt32(
                        reader["IdAbonado"]),

                NombreCompleto =
                    nombreCompleto,

                Correo =
                    reader["CorreoElectronico"]?.ToString()
                    ?? "",

                Telefono =
                    reader["Telefono"]?.ToString()
                    ?? "",

                NIS =
                    reader["NIS"]?.ToString()
                    ?? "",

                NumeroPaja =
                    reader["NumeroPaja"]?.ToString()
                    ?? "",

                NumeroFinca =
                    reader["NumeroFinca"]?.ToString()
                    ?? "",

                NumeroPlano =
                    reader["NumeroPlano"]?.ToString()
                    ?? "",

                Direccion =
                    reader["Direccion"]?.ToString()
                    ?? "",

                Tarifa =
                    reader["Tarifa"]?.ToString()
                    ?? "",

                Sector =
                    reader["Sector"]?.ToString()
                    ?? ""
            };


        // 6. Muestra la  vista

        return View(
            modelo);
    }
}
