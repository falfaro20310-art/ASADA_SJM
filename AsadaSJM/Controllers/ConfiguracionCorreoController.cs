using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using AsadaSJM.Models;

namespace AsadaSJM.Controllers;

public class ConfiguracionCorreoController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IDataProtector _protector;

    public ConfiguracionCorreoController(
        IConfiguration configuration,
        IDataProtectionProvider dataProtectionProvider)
    {
        _configuration = configuration;

        // Un "propósito" fijo para que solo este código pueda desencriptar
        _protector = dataProtectionProvider.CreateProtector(
            "AsadaSJM.ConfiguracionCorreo.Password");
    }


    // =========================================================
    // GET: mostrar configuración actual (sin exponer la contraseña)
    // =========================================================

    [HttpGet]
    public IActionResult Index()
    {
        string connectionString =
            _configuration.GetConnectionString("DefaultConnection")!;

        var modelo = new ConfiguracionCorreoViewModel();

        using var connection = new SqlConnection(connectionString);
        using var command = new SqlCommand(
            "SELECT Host, Port, Username, FromEmail, FromName FROM ConfiguracionCorreo WHERE Id = 1",
            connection);

        connection.Open();

        using var reader = command.ExecuteReader();

        if (reader.Read())
        {
            modelo.Host = reader["Host"].ToString() ?? "";
            modelo.Port = Convert.ToInt32(reader["Port"]);
            modelo.Username = reader["Username"].ToString() ?? "";
            modelo.FromEmail = reader["FromEmail"].ToString() ?? "";
            modelo.FromName = reader["FromName"].ToString() ?? "";
        }

        return View(modelo);
    }


    // =========================================================
    // POST: guardar configuración
    // =========================================================

    [HttpPost]
    public IActionResult Index(ConfiguracionCorreoViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Host) ||
            string.IsNullOrWhiteSpace(model.Username) ||
            string.IsNullOrWhiteSpace(model.FromEmail) ||
            string.IsNullOrWhiteSpace(model.FromName) ||
            model.Port == null)
        {
            ModelState.AddModelError(string.Empty, "Complete todos los campos.");
            return View(model);
        }


        // -----------------------------------------------------
        // Limpiar espacios y formato antes de guardar
        // -----------------------------------------------------

        model.Host = model.Host.Trim();
        model.Username = model.Username.Trim();
        model.FromEmail = model.FromEmail.Trim();
        model.FromName = model.FromName.Trim();

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            model.Password = model.Password.Replace(" ", "").Trim();
        }


        string connectionString =
            _configuration.GetConnectionString("DefaultConnection")!;

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        // Si el admin dejó la contraseña en blanco, mantenemos la que ya está guardada
        string passwordEncriptadoFinal;

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            passwordEncriptadoFinal = _protector.Protect(model.Password);
        }
        else
        {
            using var buscarActual = new SqlCommand(
                "SELECT PasswordEncriptado FROM ConfiguracionCorreo WHERE Id = 1",
                connection);

            var actual = buscarActual.ExecuteScalar();

            if (actual == null)
            {
                ModelState.AddModelError(string.Empty, "Debe ingresar la contraseña.");
                return View(model);
            }

            passwordEncriptadoFinal = actual.ToString()!;
        }

        using var command = new SqlCommand(@"
            MERGE ConfiguracionCorreo AS destino
            USING (SELECT 1 AS Id) AS origen
            ON destino.Id = origen.Id
            WHEN MATCHED THEN
                UPDATE SET
                    Host = @Host,
                    Port = @Port,
                    Username = @Username,
                    PasswordEncriptado = @PasswordEncriptado,
                    FromEmail = @FromEmail,
                    FromName = @FromName,
                    FechaModificacion = GETDATE()
            WHEN NOT MATCHED THEN
                INSERT (Id, Host, Port, Username, PasswordEncriptado, FromEmail, FromName)
                VALUES (1, @Host, @Port, @Username, @PasswordEncriptado, @FromEmail, @FromName);",
            connection);

        command.Parameters.AddWithValue("@Host", model.Host);
        command.Parameters.AddWithValue("@Port", model.Port!.Value);
        command.Parameters.AddWithValue("@Username", model.Username);
        command.Parameters.AddWithValue("@PasswordEncriptado", passwordEncriptadoFinal);
        command.Parameters.AddWithValue("@FromEmail", model.FromEmail);
        command.Parameters.AddWithValue("@FromName", model.FromName);

        command.ExecuteNonQuery();

        TempData["ConfigGuardada"] = "La configuración de correo fue guardada correctamente.";

        return RedirectToAction(nameof(Index));
    }
}
