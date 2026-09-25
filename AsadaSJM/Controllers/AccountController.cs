using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.Text;
using AsadaSJM.Models;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Claims;

namespace AsadaSJM.Controllers;

public class AccountController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IDataProtector _protector;

    public AccountController(
        IConfiguration configuration,
        IDataProtectionProvider dataProtectionProvider)
    {
        _configuration = configuration;

        _protector = dataProtectionProvider.CreateProtector(
            "AsadaSJM.ConfiguracionCorreo.Password");
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
        // 1. Validar campos
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
        // 2. Obtener conexión
        // -----------------------------------------------------

        string? connectionString =
            _configuration.GetConnectionString(
                "DefaultConnection");

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
            new SqlCommand(
                "SP_LoginUsuario",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.AddWithValue(
            "@Correo",
            correo);

        await connection.OpenAsync();

        using SqlDataReader reader =
            await command.ExecuteReaderAsync();


        // -----------------------------------------------------
        // 4. Verificar usuario
        // -----------------------------------------------------

        if (await reader.ReadAsync())
        {
            string passwordHash =
                reader["PasswordHash"]?.ToString() ?? "";

            // -------------------------------------------------
            // Verificar contraseña
            // -------------------------------------------------

            bool passwordCorrecta;

            try
            {
                passwordCorrecta =
                    BCrypt.Net.BCrypt.Verify(
                        contrasena,
                        passwordHash);
            }
            catch
            {
                passwordCorrecta = false;
            }


            if (passwordCorrecta)
            {
                // ---------------------------------------------
                // 5. Obtener nombre
                // ---------------------------------------------

                string nombre =
                    reader["Nombres"]?.ToString()
                    ?? correo;


                // ---------------------------------------------
                // 6. Obtener rol
                // ---------------------------------------------

                string rol =
                    reader["RoleName"]?.ToString()
                    ?? "Abonado";

                rol = rol.Trim();


                // ---------------------------------------------
                // Normalizar nombres de roles
                // ---------------------------------------------

                if (rol.Equals(
                    "Admin",
                    StringComparison.OrdinalIgnoreCase))
                {
                    rol = "Administrador";
                }


                // ---------------------------------------------
                // 7. Crear Claims
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


                var principal =
                    new ClaimsPrincipal(
                        claimsIdentity);


                var authProperties =
                    new AuthenticationProperties
                    {
                        IsPersistent = false
                    };


                // ---------------------------------------------
                // 8. Crear sesión
                // ---------------------------------------------

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    authProperties);


                // ---------------------------------------------
                // 9. Redireccionar según rol
                // ---------------------------------------------

                if (rol.Equals(
                    "Administrador",
                    StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction(
                        "Index",
                        "Home");
                }


                // Abonado y Operativo
                return RedirectToAction(
                    "Index",
                    "Portal");
            }
        }


        // -----------------------------------------------------
        // 10. Login incorrecto
        // -----------------------------------------------------

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
        string ConfirmarContrasena,
        string RolId)
    {
        // -----------------------------------------------------
        // 1. Validar campos
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(Nombres) ||
            string.IsNullOrWhiteSpace(PrimerApellido) ||
            string.IsNullOrWhiteSpace(SegundoApellido) ||
            string.IsNullOrWhiteSpace(Identificacion) ||
            string.IsNullOrWhiteSpace(Telefono) ||
            string.IsNullOrWhiteSpace(Correo) ||
            string.IsNullOrWhiteSpace(Contrasena) ||
            string.IsNullOrWhiteSpace(ConfirmarContrasena) ||
            string.IsNullOrWhiteSpace(RolId))
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
            _configuration.GetConnectionString(
                "DefaultConnection");

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
        // 4. Verificar correo
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
        // 5. Verificar identificación
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
            "@RolId",
            RolId);

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


        // -----------------------------------------------------
        // 8. Ejecutar registro UNA SOLA VEZ
        // -----------------------------------------------------

        command.ExecuteNonQuery();


        // -----------------------------------------------------
        // 9. Registro exitoso
        // -----------------------------------------------------

        TempData["RegistroExitoso"] =
            "La cuenta fue creada correctamente. Ya puede iniciar sesión.";

        return RedirectToAction(
            "Login");
    }


    // =========================================================
    // RECUPERACIÓN DE CONTRASEÑA
    // =========================================================

    [HttpGet]
    public IActionResult RecuperarContrasena()
    {
        return View();
    }


    [HttpPost]
    public IActionResult RecuperarContrasena(
        string correo)
    {
        // -----------------------------------------------------
        // 1. Validar correo
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(correo))
        {
            ModelState.AddModelError(
                string.Empty,
                "Debe ingresar su correo electrónico.");

            return View();
        }


        // -----------------------------------------------------
        // 2. Obtener conexión
        // -----------------------------------------------------

        string? connectionString =
            _configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            ModelState.AddModelError(
                string.Empty,
                "No se encontró la conexión a la base de datos.");

            return View();
        }


        // -----------------------------------------------------
        // 3. Generar token
        // -----------------------------------------------------

        byte[] tokenBytes =
            RandomNumberGenerator.GetBytes(32);

        string token =
            WebEncoders.Base64UrlEncode(
                tokenBytes);


        // -----------------------------------------------------
        // 4. Generar hash
        // -----------------------------------------------------

        string tokenHash =
            GenerarHashToken(token);


        // -----------------------------------------------------
        // 5. Expiración
        // -----------------------------------------------------

        DateTime fechaExpiracion =
            DateTime.Now.AddMinutes(30);


        // -----------------------------------------------------
        // 6. Ejecutar procedimiento
        // -----------------------------------------------------

        using SqlConnection connection =
            new SqlConnection(
                connectionString);

        using SqlCommand command =
            new SqlCommand(
                "SP_CrearRecuperacionContrasena",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.AddWithValue(
            "@Correo",
            correo);

        command.Parameters.AddWithValue(
            "@TokenHash",
            tokenHash);

        command.Parameters.AddWithValue(
            "@FechaExpiracion",
            fechaExpiracion);

        connection.Open();

        int resultado =
            Convert.ToInt32(
                command.ExecuteScalar());


        // -----------------------------------------------------
        // 7. Enviar correo
        // -----------------------------------------------------

        if (resultado == 1)
        {
            EnviarCorreoRecuperacion(
                correo,
                token);
        }


        // -----------------------------------------------------
        // 8. Mensaje
        // -----------------------------------------------------

        TempData["MensajeRecuperacion"] =
            "Si el correo está registrado, recibirá un enlace para restablecer su contraseña.";

        return RedirectToAction(
            nameof(RecuperarContrasena));
    }


    // =========================================================
    // GENERAR HASH DEL TOKEN
    // =========================================================

    private string GenerarHashToken(
        string token)
    {
        byte[] bytes =
            Encoding.UTF8.GetBytes(token);

        byte[] hash =
            SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }


    // =========================================================
    // ENVIAR CORREO
    // =========================================================

    private void EnviarCorreoRecuperacion(
        string correo,
        string token)
    {
        string connectionStringCorreo =
            _configuration.GetConnectionString(
                "DefaultConnection")!;

        string host = "";
        string username = "";
        string fromEmail = "";
        string fromName = "";

        int port = 587;

        string passwordEncriptado = "";


        // -----------------------------------------------------
        // 1. Obtener configuración
        // -----------------------------------------------------

        using (var connCorreo =
            new SqlConnection(
                connectionStringCorreo))

        using (var cmdCorreo =
            new SqlCommand(
                @"SELECT Host,
                         Port,
                         Username,
                         PasswordEncriptado,
                         FromEmail,
                         FromName
                  FROM ConfiguracionCorreo
                  WHERE Id = 1",
                connCorreo))
        {
            connCorreo.Open();

            using var readerCorreo =
                cmdCorreo.ExecuteReader();

            if (!readerCorreo.Read())
            {
                throw new InvalidOperationException(
                    "No hay configuración de correo definida.");
            }

            host =
                readerCorreo["Host"]?.ToString()
                ?? "";

            port =
                Convert.ToInt32(
                    readerCorreo["Port"]);

            username =
                readerCorreo["Username"]?.ToString()
                ?? "";

            passwordEncriptado =
                readerCorreo["PasswordEncriptado"]?.ToString()
                ?? "";

            fromEmail =
                readerCorreo["FromEmail"]?.ToString()
                ?? "";

            fromName =
                readerCorreo["FromName"]?.ToString()
                ?? "";
        }


        // -----------------------------------------------------
        // 2. Desencriptar contraseña
        // -----------------------------------------------------

        string password =
            _protector.Unprotect(
                passwordEncriptado);


        // -----------------------------------------------------
        // 3. Crear enlace
        // -----------------------------------------------------

        string esquema =
            Request.Scheme;

        string hostActual =
            Request.Host.ToString();

        string enlace =
            $"{esquema}://{hostActual}/Account/RestablecerContrasena?token={Uri.EscapeDataString(token)}";


        // -----------------------------------------------------
        // 4. Crear mensaje
        // -----------------------------------------------------

        var mensaje =
            new MimeMessage();

        mensaje.From.Add(
            new MailboxAddress(
                fromName,
                fromEmail));

        mensaje.To.Add(
            MailboxAddress.Parse(
                correo));

        mensaje.Subject =
            "Recuperación de contraseña - ASADA SJM";


        mensaje.Body =
            new TextPart("html")
            {
                Text = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>

                        <h2>
                            ASADA San José de la Montaña
                        </h2>

                        <p>
                            Hemos recibido una solicitud para
                            restablecer su contraseña.
                        </p>

                        <p>
                            Para cambiar su contraseña,
                            haga clic en el siguiente enlace:
                        </p>

                        <p>
                            <a href='{enlace}'>
                                Restablecer contraseña
                            </a>
                        </p>

                        <p>
                            Este enlace será válido durante
                            30 minutos.
                        </p>

                        <p>
                            Si usted no realizó esta solicitud,
                            puede ignorar este correo.
                        </p>

                    </body>
                    </html>"
            };


        // -----------------------------------------------------
        // 5. SMTP
        // -----------------------------------------------------

        using var smtp =
            new SmtpClient();

        smtp.Connect(
            host,
            port,
            MailKit.Security.SecureSocketOptions.StartTls);


        // -----------------------------------------------------
        // 6. Autenticación
        // -----------------------------------------------------

        smtp.Authenticate(
            username,
            password);


        // -----------------------------------------------------
        // 7. Enviar
        // -----------------------------------------------------

        smtp.Send(
            mensaje);


        // -----------------------------------------------------
        // 8. Desconectar
        // -----------------------------------------------------

        smtp.Disconnect(
            true);
    }


    // =========================================================
    // RESTABLECER CONTRASEÑA - GET
    // =========================================================

    [HttpGet]
    public IActionResult RestablecerContrasena(
        string token)
    {
        // -----------------------------------------------------
        // 1. Validar token
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(
                "El enlace de recuperación no es válido.");
        }


        // -----------------------------------------------------
        // 2. Hash
        // -----------------------------------------------------

        string tokenHash =
            GenerarHashToken(token);


        // -----------------------------------------------------
        // 3. Conexión
        // -----------------------------------------------------

        string? connectionString =
            _configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            return BadRequest(
                "No se encontró la conexión a la base de datos.");
        }


        // -----------------------------------------------------
        // 4. Validar token
        // -----------------------------------------------------

        using SqlConnection connection =
            new SqlConnection(
                connectionString);

        using SqlCommand command =
            new SqlCommand(
                "SP_ValidarTokenRecuperacion",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.AddWithValue(
            "@TokenHash",
            tokenHash);

        connection.Open();

        using SqlDataReader reader =
            command.ExecuteReader();


        // -----------------------------------------------------
        // 5. Token inválido
        // -----------------------------------------------------

        if (!reader.Read())
        {
            return View(
                "TokenInvalido");
        }


        // -----------------------------------------------------
        // 6. Mostrar formulario
        // -----------------------------------------------------

        var modelo =
            new RestablecerContrasenaViewModel
            {
                Token = token
            };

        return View(
            modelo);
    }


    // =========================================================
    // RESTABLECER CONTRASEÑA - POST
    // =========================================================

    [HttpPost]
    public IActionResult RestablecerContrasena(
        RestablecerContrasenaViewModel model)
    {
        // -----------------------------------------------------
        // 1. Validar modelo
        // -----------------------------------------------------

        if (model == null ||
            string.IsNullOrWhiteSpace(model.Token))
        {
            ModelState.AddModelError(
                string.Empty,
                "El enlace de recuperación no es válido.");

            return View(model);
        }


        // -----------------------------------------------------
        // 2. Validar contraseña
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(
                model.NuevaContrasena) ||
            string.IsNullOrWhiteSpace(
                model.ConfirmarContrasena))
        {
            ModelState.AddModelError(
                string.Empty,
                "Debe ingresar y confirmar la nueva contraseña.");

            return View(model);
        }


        // -----------------------------------------------------
        // 3. Verificar coincidencia
        // -----------------------------------------------------

        if (model.NuevaContrasena !=
            model.ConfirmarContrasena)
        {
            ModelState.AddModelError(
                string.Empty,
                "Las contraseñas no coinciden.");

            return View(model);
        }


        // -----------------------------------------------------
        // 4. Hash token
        // -----------------------------------------------------

        string tokenHash =
            GenerarHashToken(
                model.Token);


        // -----------------------------------------------------
        // 5. Conexión
        // -----------------------------------------------------

        string? connectionString =
            _configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            ModelState.AddModelError(
                string.Empty,
                "No se encontró la conexión a la base de datos.");

            return View(model);
        }


        // -----------------------------------------------------
        // 6. Validar token nuevamente
        // -----------------------------------------------------

        int idRecuperacion;
        string idNetUser;


        using (SqlConnection connection =
            new SqlConnection(
                connectionString))
        {
            using SqlCommand command =
                new SqlCommand(
                    "SP_ValidarTokenRecuperacion",
                    connection);

            command.CommandType =
                CommandType.StoredProcedure;

            command.Parameters.AddWithValue(
                "@TokenHash",
                tokenHash);

            connection.Open();

            using SqlDataReader reader =
                command.ExecuteReader();


            if (!reader.Read())
            {
                ModelState.AddModelError(
                    string.Empty,
                    "El enlace no es válido o ya expiró.");

                return View(model);
            }


            idRecuperacion =
                Convert.ToInt32(
                    reader["IdRecuperacion"]);

            idNetUser =
                reader["IdNetUser"]?.ToString()
                ?? "";
        }


        // -----------------------------------------------------
        // 7. Hash nueva contraseña
        // -----------------------------------------------------

        string passwordHash =
            BCrypt.Net.BCrypt.HashPassword(
                model.NuevaContrasena);


        // -----------------------------------------------------
        // 8. Cambiar contraseña
        // -----------------------------------------------------

        using (SqlConnection connection =
            new SqlConnection(
                connectionString))
        {
            using SqlCommand command =
                new SqlCommand(
                    "SP_CambiarContrasena",
                    connection);

            command.CommandType =
                CommandType.StoredProcedure;

            command.Parameters.AddWithValue(
                "@IdRecuperacion",
                idRecuperacion);

            command.Parameters.AddWithValue(
                "@IdNetUser",
                idNetUser);

            command.Parameters.AddWithValue(
                "@PasswordHash",
                passwordHash);

            connection.Open();

            int resultado =
                Convert.ToInt32(
                    command.ExecuteScalar());


            // -------------------------------------------------
            // 9. Verificar resultado
            // -------------------------------------------------

            if (resultado != 1)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No fue posible cambiar la contraseña.");

                return View(model);
            }
        }


        // -----------------------------------------------------
        // 10. Éxito
        // -----------------------------------------------------

        TempData["RegistroExitoso"] =
            "La contraseña fue cambiada correctamente. Ya puede iniciar sesión.";

        return RedirectToAction(
            "Login");
    }
}