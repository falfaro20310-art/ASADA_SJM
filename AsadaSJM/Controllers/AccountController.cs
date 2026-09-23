using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AsadaSJM.Controllers;

public class AccountController : Controller
{
    private readonly IConfiguration _configuration;

    public AccountController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string correo, string contrasena)
    {
        // Validar que se hayan ingresado los datos
        if (string.IsNullOrWhiteSpace(correo) ||
            string.IsNullOrWhiteSpace(contrasena))
        {
            ModelState.AddModelError(
                string.Empty,
                "Debe ingresar el correo y la contraseña.");

            return View();
        }

        // Obtener la conexión configurada en appsettings.json
        string? connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            ModelState.AddModelError(
                string.Empty,
                "No se encontró la conexión a la base de datos.");

            return View();
        }

        // Crear conexión con SQL Server
        using SqlConnection connection =
            new SqlConnection(connectionString);

        // Ejecutar el procedimiento almacenado
        using SqlCommand command =
            new SqlCommand("SP_LoginUsuario", connection);

        command.CommandType = CommandType.StoredProcedure;

        // Enviar el correo al procedimiento almacenado
        command.Parameters.AddWithValue("@Correo", correo);

        // Abrir conexión
        connection.Open();

        // Ejecutar procedimiento
        using SqlDataReader reader = command.ExecuteReader();

        // Verificar si se encontró el usuario
        if (reader.Read())
        {
            // Obtener el hash de la contraseña almacenado en la base de datos
            string passwordHash =
                reader["PasswordHash"]?.ToString() ?? "";

            // Comparar la contraseña escrita con el hash
            bool passwordCorrecta =
                BCrypt.Net.BCrypt.Verify(contrasena, passwordHash);

            // Si la contraseña es correcta
            if (passwordCorrecta)
            {
                return RedirectToAction("Index", "Home");
            }
        }

        // Si el usuario no existe o la contraseña es incorrecta
        ModelState.AddModelError(
            string.Empty,
            "Correo o contraseña incorrectos.");

        return View();
    }
}
