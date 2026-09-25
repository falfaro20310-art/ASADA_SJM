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
            new SqlCommand(
                "SP_LoginUsuario",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.AddWithValue(
            "@Correo",
            correo);

        connection.Open();

        using SqlDataReader reader =
            command.ExecuteReader();

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
                return RedirectToAction(
                    "Index",
                    "Home");
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
        string ConfirmarContrasena,
        string RolId)
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
            string.IsNullOrWhiteSpace(ConfirmarContrasena) ||
            string.IsNullOrWhiteSpace(RolId))
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
        // 6. Verificar si la identificación ya existe
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
        // 7. Generar hash de la contraseña
        // -----------------------------------------------------

        string passwordHash =
            BCrypt.Net.BCrypt.HashPassword(
                Contrasena);


        // -----------------------------------------------------
        // 8. Ejecutar SP_RegistrarUsuario
        // -----------------------------------------------------

        using SqlCommand command =
            new SqlCommand(
                "SP_RegistrarUsuario",
                connection);

        command.Parameters.AddWithValue(
            "@RolId",
            RolId);

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


        // -----------------------------------------------------
        // 9. Ejecutar registro
        // -----------------------------------------------------

        command.ExecuteNonQuery();


        // -----------------------------------------------------
        // 10. Mostrar mensaje de éxito
        // -----------------------------------------------------

        TempData["RegistroExitoso"] =
            "La cuenta fue creada correctamente. Ya puede iniciar sesión.";

        return RedirectToAction("Login");
    }


    // =========================================================
    // RECUPERACIÓN DE CONTRASEÑA
    // =========================================================

    // GET: Account/RecuperarContrasena
    [HttpGet]
    public IActionResult RecuperarContrasena()
    {
        return View();
    }


    // POST: Account/RecuperarContrasena
    [HttpPost]
    public IActionResult RecuperarContrasena(string correo)
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
        // 4. Generar hash del token
        // -----------------------------------------------------

        string tokenHash =
            GenerarHashToken(token);


        // -----------------------------------------------------
        // 5. Definir expiración
        // -----------------------------------------------------

        DateTime fechaExpiracion =
            DateTime.Now.AddMinutes(30);


        // -----------------------------------------------------
        // 6. Ejecutar SP_CrearRecuperacionContrasena
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
        // 7. Si existe el usuario, enviar correo
        // -----------------------------------------------------

        if (resultado == 1)
        {
            EnviarCorreoRecuperacion(
                correo,
                token);
        }


        // -----------------------------------------------------
        // 8. Mostrar mensaje
        // -----------------------------------------------------

        TempData["MensajeRecuperacion"] =
            "Si el correo está registrado, recibirá un enlace para restablecer su contraseña.";

        return RedirectToAction(
            nameof(RecuperarContrasena));
    }


    // =========================================================
    // GENERAR HASH DEL TOKEN
    // =========================================================

    private string GenerarHashToken(string token)
    {
        byte[] bytes =
            Encoding.UTF8.GetBytes(token);

        byte[] hash =
            SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }


    // =========================================================
    // ENVIAR CORREO DE RECUPERACIÓN
    // =========================================================

    private void EnviarCorreoRecuperacion(
        string correo,
        string token)
    {
        // -----------------------------------------------------
        // 1. Obtener configuración del correo (desde la BD)
        // -----------------------------------------------------

        string connectionStringCorreo =
            _configuration.GetConnectionString("DefaultConnection")!;

        string host = "", username = "", fromEmail = "", fromName = "";
        int port = 587;
        string passwordEncriptado = "";

        using (var connCorreo = new SqlConnection(connectionStringCorreo))
        using (var cmdCorreo = new SqlCommand(
            "SELECT Host, Port, Username, PasswordEncriptado, FromEmail, FromName FROM ConfiguracionCorreo WHERE Id = 1",
            connCorreo))
        {
            connCorreo.Open();
            using var readerCorreo = cmdCorreo.ExecuteReader();

            if (!readerCorreo.Read())
            {
                throw new InvalidOperationException(
                    "No hay configuración de correo definida. Configúrela en /ConfiguracionCorreo.");
            }

            host = readerCorreo["Host"].ToString() ?? "";
            port = Convert.ToInt32(readerCorreo["Port"]);
            username = readerCorreo["Username"].ToString() ?? "";
            passwordEncriptado = readerCorreo["PasswordEncriptado"].ToString() ?? "";
            fromEmail = readerCorreo["FromEmail"].ToString() ?? "";
            fromName = readerCorreo["FromName"].ToString() ?? "";
        }

        string password = _protector.Unprotect(passwordEncriptado);


        // -----------------------------------------------------
        // 2. Crear URL de recuperación
        // -----------------------------------------------------

        string esquema =
            Request.Scheme;

        string hostActual =
            Request.Host.ToString();

        string enlace =
            $"{esquema}://{hostActual}/Account/RestablecerContrasena?token={Uri.EscapeDataString(token)}";


        // -----------------------------------------------------
        // 3. Crear mensaje
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


        // -----------------------------------------------------
        // 4. Contenido del correo
        // -----------------------------------------------------

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
        // 5. Conectar con SMTP
        // -----------------------------------------------------

        using var smtp =
            new SmtpClient();

        smtp.Connect(
            host,
            port,
            MailKit.Security.SecureSocketOptions.StartTls);


        // -----------------------------------------------------
        // 6. Autenticarse
        // -----------------------------------------------------

        smtp.Authenticate(
            username,
            password);


        // -----------------------------------------------------
        // 7. Enviar correo
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
    // RESTABLECER CONTRASEÑA
    // =========================================================

    // GET: Account/RestablecerContrasena
    [HttpGet]
    public IActionResult RestablecerContrasena(
        string token)
    {
        // -----------------------------------------------------
        // 1. Verificar token recibido
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(
                "El enlace de recuperación no es válido.");
        }


        // -----------------------------------------------------
        // 2. Generar hash
        // -----------------------------------------------------

        string tokenHash =
            GenerarHashToken(token);


        // -----------------------------------------------------
        // 3. Obtener conexión
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
        // 4. Validar token mediante SP
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
        // 5. Si el token no es válido
        // -----------------------------------------------------

        if (!reader.Read())
        {
            return View(
                "TokenInvalido");
        }


        // -----------------------------------------------------
        // 6. Mostrar formulario de nueva contraseña
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
    // POST RESTABLECER CONTRASEÑA
    // =========================================================

    [HttpPost]
    public IActionResult RestablecerContrasena(
        RestablecerContrasenaViewModel model)
    {
        // -----------------------------------------------------
        // 1. Validar token
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(model.Token))
        {
            ModelState.AddModelError(
                string.Empty,
                "El enlace de recuperación no es válido.");

            return View(model);
        }


        // -----------------------------------------------------
        // 2. Validar nueva contraseña
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
        // 3. Verificar que coincidan
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
        // 4. Generar hash del token
        // -----------------------------------------------------

        string tokenHash =
            GenerarHashToken(
                model.Token);


        // -----------------------------------------------------
        // 5. Obtener conexión
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
        // 7. Generar nuevo hash de contraseña
        // -----------------------------------------------------

        string passwordHash =
            BCrypt.Net.BCrypt.HashPassword(
                model.NuevaContrasena);


        // -----------------------------------------------------
        // 8. Cambiar contraseña mediante SP
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
        // 10. Mostrar mensaje de éxito
        // -----------------------------------------------------

        TempData["RegistroExitoso"] =
            "La contraseña fue cambiada correctamente. Ya puede iniciar sesión.";

        return RedirectToAction(
            "Login");
    }
}