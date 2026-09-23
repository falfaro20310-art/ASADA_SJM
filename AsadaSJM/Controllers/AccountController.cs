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

    // =========================================================
    // LOGIN
    // =========================================================

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

        string? connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            ModelState.AddModelError(
                string.Empty,
                "No se encontró la conexión a la base de datos.");

            return View();
        }

        using SqlConnection connection =
            new SqlConnection(connectionString);

        using SqlCommand command =
            new SqlCommand("SP_LoginUsuario", connection);

        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.AddWithValue("@Correo", correo);

        connection.Open();

        using SqlDataReader reader = command.ExecuteReader();

        if (reader.Read())
        {
            string passwordHash =
                reader["PasswordHash"]?.ToString() ?? "";

            bool passwordCorrecta =
                BCrypt.Net.BCrypt.Verify(contrasena, passwordHash);

            if (passwordCorrecta)
            {
                return RedirectToAction("Index", "Home");
            }
        }

        ModelState.AddModelError(
            string.Empty,
            "Correo o contraseña incorrectos.");

        return View();
    }


    // =========================================================
    // REGISTRO
    // =========================================================

    // GET: Account/Registro
    [HttpGet]
    public IActionResult Registro()
    {
        return View();
    }


    // POST: Account/Registro
    [HttpPost]
    public IActionResult Registro(
        string Nombres,
        string PrimerApellido,
        string SegundoApellido,
        string Identificacion,
        string Telefono,
        string Correo,
        string Contrasena,
        string ConfirmarContrasena)
    {
        // -----------------------------------------------------
        // 1. Validar campos obligatorios
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(Nombres) ||
            string.IsNullOrWhiteSpace(PrimerApellido) ||
            string.IsNullOrWhiteSpace(SegundoApellido) ||
            string.IsNullOrWhiteSpace(Identificacion) ||
            string.IsNullOrWhiteSpace(Telefono) ||
            string.IsNullOrWhiteSpace(Correo) ||
            string.IsNullOrWhiteSpace(Contrasena) ||
            string.IsNullOrWhiteSpace(ConfirmarContrasena))
        {
            ModelState.AddModelError(
                string.Empty,
                "Todos los campos son obligatorios.");

            return View();
        }


        // -----------------------------------------------------
        // 2. Verificar que las contraseñas coincidan
        // -----------------------------------------------------

        if (Contrasena != ConfirmarContrasena)
        {
            ModelState.AddModelError(
                string.Empty,
                "Las contraseñas no coinciden.");

            return View();
        }


        // -----------------------------------------------------
        // 3. Obtener conexión
        // -----------------------------------------------------

        string? connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            ModelState.AddModelError(
                string.Empty,
                "No se encontró la conexión a la base de datos.");

            return View();
        }


        // -----------------------------------------------------
        // 4. Conectar a SQL Server
        // -----------------------------------------------------

        using SqlConnection connection =
            new SqlConnection(connectionString);


        // -----------------------------------------------------
        // 5. Verificar si el correo ya existe
        // -----------------------------------------------------

        string consultaCorreo = @"
            SELECT COUNT(*)
            FROM AspNetUsers
            WHERE Email = @Correo";

        using (SqlCommand verificarCorreo =
            new SqlCommand(consultaCorreo, connection))
        {
            verificarCorreo.Parameters.AddWithValue(
                "@Correo",
                Correo);

            connection.Open();

            int cantidadCorreo =
                Convert.ToInt32(verificarCorreo.ExecuteScalar());

            if (cantidadCorreo > 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Ya existe un usuario registrado con ese correo.");

                return View();
            }
        }


        // -----------------------------------------------------
        // 6. Verificar si la identificación ya existe
        // -----------------------------------------------------

        string consultaIdentificacion = @"
            SELECT COUNT(*)
            FROM USUARIO
            WHERE Identificacion = @Identificacion";

        using (SqlCommand verificarIdentificacion =
            new SqlCommand(consultaIdentificacion, connection))
        {
            verificarIdentificacion.Parameters.AddWithValue(
                "@Identificacion",
                Identificacion);

            int cantidadIdentificacion =
                Convert.ToInt32(
                    verificarIdentificacion.ExecuteScalar());

            if (cantidadIdentificacion > 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Ya existe un usuario registrado con esa identificación.");

                return View();
            }
        }


        // -----------------------------------------------------
        // 7. Generar hash de la contraseña
        // -----------------------------------------------------

        string passwordHash =
            BCrypt.Net.BCrypt.HashPassword(Contrasena);


        // 8. Ejecutar SP_RegistrarUsuario

        using SqlCommand command =
            new SqlCommand(
                "SP_RegistrarUsuario",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;


        command.Parameters.AddWithValue(
            "@Correo",
            Correo);

        command.Parameters.AddWithValue(
            "@PasswordHash",
            passwordHash);

        command.Parameters.AddWithValue(
            "@Nombres",
            Nombres);

        command.Parameters.AddWithValue(
            "@PrimerApellido",
            PrimerApellido);

        command.Parameters.AddWithValue(
            "@SegundoApellido",
            SegundoApellido);

        command.Parameters.AddWithValue(
            "@Identificacion",
            Identificacion);

        command.Parameters.AddWithValue(
            "@Telefono",
            Telefono);


       
        // 9. Ejecutar registro

        command.ExecuteNonQuery();


        // 10. Mostrar mensaje de éxito

        TempData["RegistroExitoso"] =
            "La cuenta fue creada correctamente. Ya puede iniciar sesión.";

        return RedirectToAction("Login");
    }
}
