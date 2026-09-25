using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

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
    public async Task<IActionResult> Login(
        string correo,
        string contrasena)
    {
        // -----------------------------------------------------
        // 1. Validar campos obligatorios
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(correo) ||
            string.IsNullOrWhiteSpace(contrasena))
        {
            ModelState.AddModelError(
                string.Empty,
                "Debe ingresar el correo y la contraseña.");

            return View();
        }

        // -----------------------------------------------------
        // 2. Obtener cadena de conexión
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
        // 3. Ejecutar SP_LoginUsuario
        // -----------------------------------------------------

        using SqlConnection connection =
            new SqlConnection(connectionString);

        using SqlCommand command =
            new SqlCommand("SP_LoginUsuario", connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.AddWithValue(
            "@Correo",
            correo);

        connection.Open();

        using SqlDataReader reader =
            command.ExecuteReader();

        // -----------------------------------------------------
        // 4. Verificar usuario y contraseña
        // -----------------------------------------------------

        if (reader.Read())
        {
            string passwordHash =
                reader["PasswordHash"]?.ToString() ?? "";

            bool passwordCorrecta =
                BCrypt.Net.BCrypt.Verify(
                    contrasena,
                    passwordHash);

            if (passwordCorrecta)
            {
                string nombre =
                    reader["Nombres"]?.ToString()
                    ?? correo;

                string rol =
                    reader["RoleName"]?.ToString()
                    ?? "";

                // ---------------------------------------------
                // 5. Crear Claims
                // ---------------------------------------------

                var claims = new List<Claim>
                {
                    new Claim(
                        ClaimTypes.Name,
                        nombre),

                    new Claim(
                        ClaimTypes.Email,
                        correo),

                    new Claim(
                        ClaimTypes.Role,
                        rol)
                };

                var claimsIdentity =
                    new ClaimsIdentity(
                        claims,
                        CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties =
                    new AuthenticationProperties
                    {
                        IsPersistent = false
                    };

                // ---------------------------------------------
                // 6. Crear sesión
                // ---------------------------------------------

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // ---------------------------------------------
                // 7. Redirigir según rol
                // ---------------------------------------------

                if (rol == "Administrador")
                {
                    return RedirectToAction(
                        "Index",
                        "Home");
                }

                // Abonado y Operativo van por ahora al Portal
                return RedirectToAction(
                    "Index",
                    "Portal");
            }
        }

        ModelState.AddModelError(
            string.Empty,
            "Correo o contraseña incorrectos.");

        return View();
    }

    // =========================================================
    // LOGOUT
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction(
            "Login",
            "Account");
    }

    // =========================================================
    // ACCESO DENEGADO
    // =========================================================

    [HttpGet]
    public IActionResult AccessDenied()
    {
        TempData["AccesoDenegado"] =
            "No tiene permisos para acceder a esa sección.";

        return RedirectToAction(
            "Index",
            "Portal");
    }

    // =========================================================
    // REGISTRO
    // =========================================================

    [HttpGet]
    public IActionResult Registro()
    {
        return View();
    }

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
        // 2. Verificar contraseñas
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

        using SqlConnection connection =
            new SqlConnection(connectionString);

        // -----------------------------------------------------
        // 4. Verificar correo existente
        // -----------------------------------------------------

        string consultaCorreo = @"
            SELECT COUNT(*)
            FROM AspNetUsers
            WHERE Email = @Correo";

        using (SqlCommand verificarCorreo =
            new SqlCommand(
                consultaCorreo,
                connection))
        {
            verificarCorreo.Parameters.AddWithValue(
                "@Correo",
                Correo);

            connection.Open();

            int cantidadCorreo =
                Convert.ToInt32(
                    verificarCorreo.ExecuteScalar());

            if (cantidadCorreo > 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Ya existe un usuario registrado con ese correo.");

                return View();
            }
        }

        // -----------------------------------------------------
        // 5. Verificar identificación existente
        // -----------------------------------------------------

        string consultaIdentificacion = @"
            SELECT COUNT(*)
            FROM USUARIO
            WHERE Identificacion = @Identificacion";

        using (SqlCommand verificarIdentificacion =
            new SqlCommand(
                consultaIdentificacion,
                connection))
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
        // 6. Generar hash BCrypt
        // -----------------------------------------------------

        string passwordHash =
            BCrypt.Net.BCrypt.HashPassword(
                Contrasena);

        // -----------------------------------------------------
        // 7. Registrar usuario
        // -----------------------------------------------------

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

        command.ExecuteNonQuery();

        // -----------------------------------------------------
        // 8. Registro exitoso
        // -----------------------------------------------------

        TempData["RegistroExitoso"] =
            "La cuenta fue creada correctamente. Ya puede iniciar sesión.";

        return RedirectToAction("Login");
    }
}