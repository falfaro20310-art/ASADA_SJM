using AsadaSJM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AsadaSJM.Controllers;

[Authorize(Roles = "Administrador")]
public class UsuariosController : Controller
{
    private readonly IConfiguration _configuration;

    public UsuariosController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // =========================================================
    // LISTADO DE USUARIOS
    // =========================================================

    public async Task<IActionResult> Index()
    {
        var usuarios = new List<UsuarioListadoViewModel>();

        string? connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return View(usuarios);
        }

        using SqlConnection connection =
            new SqlConnection(connectionString);

        const string consulta = @"
            SELECT
                AU.Id AS IdNetUser,

                LTRIM(RTRIM(
                    ISNULL(U.Nombres, '') + ' ' +
                    ISNULL(U.PrimerApellido, '') + ' ' +
                    ISNULL(U.SegundoApellido, '')
                )) AS NombreCompleto,

                AU.Email,

                ISNULL(R.Name, 'Sin rol') AS Rol,

                U.Estado

            FROM AspNetUsers AU

            INNER JOIN USUARIO U
                ON U.IdNetUser = AU.Id

            LEFT JOIN AspNetUserRoles UR
                ON UR.UserId = AU.Id

            LEFT JOIN AspNetRoles R
                ON R.Id = UR.RoleId

            ORDER BY
                U.Nombres,
                U.PrimerApellido,
                U.SegundoApellido;";

        using SqlCommand command =
            new SqlCommand(
                consulta,
                connection);

        await connection.OpenAsync();

        using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            usuarios.Add(
                new UsuarioListadoViewModel
                {
                    IdNetUser =
                        reader["IdNetUser"]?.ToString() ?? "",

                    NombreCompleto =
                        reader["NombreCompleto"]?.ToString() ?? "",

                    Correo =
                        reader["Email"]?.ToString() ?? "",

                    Rol =
                        reader["Rol"]?.ToString() ?? "",

                    Estado =
                        reader["Estado"] != DBNull.Value &&
                        Convert.ToBoolean(reader["Estado"])
                });
        }

        return View(usuarios);
    }


    // =========================================================
    // REGISTRAR ADMINISTRADOR - GET
    // =========================================================

    [HttpGet]
    public IActionResult RegistrarAdministrador()
    {
        return View();
    }


    // =========================================================
    // REGISTRAR ADMINISTRADOR - POST
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RegistrarAdministrador(
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
        // 2. Validar contraseñas
        // -----------------------------------------------------

        if (Contrasena != ConfirmarContrasena)
        {
            ModelState.AddModelError(
                string.Empty,
                "Las contraseñas no coinciden.");

            return View();
        }

        if (Contrasena.Length < 8)
        {
            ModelState.AddModelError(
                string.Empty,
                "La contraseña debe tener al menos 8 caracteres.");

            return View();
        }

        // -----------------------------------------------------
        // 3. Normalizar información
        // -----------------------------------------------------

        Nombres = Nombres.Trim();
        PrimerApellido = PrimerApellido.Trim();
        SegundoApellido = SegundoApellido.Trim();
        Identificacion = Identificacion.Trim();
        Telefono = Telefono.Trim();
        Correo = Correo.Trim().ToLowerInvariant();

        // -----------------------------------------------------
        // 4. Obtener cadena de conexión
        // -----------------------------------------------------

        string? connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            ModelState.AddModelError(
                string.Empty,
                "No se encontró la conexión a la base de datos.");

            return View();
        }

        using SqlConnection connection =
            new SqlConnection(connectionString);

        connection.Open();

        using SqlTransaction transaction =
            connection.BeginTransaction();

        try
        {
            // -------------------------------------------------
            // 5. Verificar correo duplicado
            // -------------------------------------------------

            const string consultaCorreo = @"
                SELECT COUNT(*)
                FROM AspNetUsers
                WHERE LOWER(Email) = LOWER(@Correo);";

            using (SqlCommand verificarCorreo =
                new SqlCommand(
                    consultaCorreo,
                    connection,
                    transaction))
            {
                verificarCorreo.Parameters.Add(
                    "@Correo",
                    SqlDbType.NVarChar,
                    256).Value = Correo;

                int cantidadCorreo =
                    Convert.ToInt32(
                        verificarCorreo.ExecuteScalar());

                if (cantidadCorreo > 0)
                {
                    transaction.Rollback();

                    ModelState.AddModelError(
                        string.Empty,
                        "Ya existe un usuario registrado con ese correo.");

                    return View();
                }
            }

            // -------------------------------------------------
            // 6. Verificar identificación duplicada
            // -------------------------------------------------

            const string consultaIdentificacion = @"
                SELECT COUNT(*)
                FROM USUARIO
                WHERE Identificacion = @Identificacion;";

            using (SqlCommand verificarIdentificacion =
                new SqlCommand(
                    consultaIdentificacion,
                    connection,
                    transaction))
            {
                verificarIdentificacion.Parameters.Add(
                    "@Identificacion",
                    SqlDbType.VarChar,
                    20).Value = Identificacion;

                int cantidadIdentificacion =
                    Convert.ToInt32(
                        verificarIdentificacion.ExecuteScalar());

                if (cantidadIdentificacion > 0)
                {
                    transaction.Rollback();

                    ModelState.AddModelError(
                        string.Empty,
                        "Ya existe un usuario registrado con esa identificación.");

                    return View();
                }
            }

            // -------------------------------------------------
            // 7. Verificar rol Administrador
            // -------------------------------------------------

            const string consultaRol = @"
                SELECT COUNT(*)
                FROM AspNetRoles
                WHERE Id = 'ROL-ADMIN'
                  AND Name = 'Administrador';";

            using (SqlCommand verificarRol =
                new SqlCommand(
                    consultaRol,
                    connection,
                    transaction))
            {
                int existeRol =
                    Convert.ToInt32(
                        verificarRol.ExecuteScalar());

                if (existeRol == 0)
                {
                    transaction.Rollback();

                    ModelState.AddModelError(
                        string.Empty,
                        "No se encontró el rol Administrador.");

                    return View();
                }
            }

            // -------------------------------------------------
            // 8. Generar identificador y contraseña segura
            // -------------------------------------------------

            string userId =
                Guid.NewGuid().ToString();

            string passwordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    Contrasena);

            string securityStamp =
                Guid.NewGuid().ToString();

            // -------------------------------------------------
            // 9. Crear usuario de autenticación
            // -------------------------------------------------

            const string insertarAspNetUser = @"
                INSERT INTO AspNetUsers
                (
                    Id,
                    Email,
                    EmailConfirmed,
                    PasswordHash,
                    SecurityStamp,
                    PhoneNumber,
                    PhoneNumberConfirmed,
                    TwoFactorEnabled,
                    LockoutEndDateUtc,
                    LockoutEnabled,
                    AccessFailedCount,
                    UserName
                )
                VALUES
                (
                    @Id,
                    @Correo,
                    1,
                    @PasswordHash,
                    @SecurityStamp,
                    @Telefono,
                    0,
                    0,
                    NULL,
                    1,
                    0,
                    @Correo
                );";

            using (SqlCommand insertarUsuarioIdentity =
                new SqlCommand(
                    insertarAspNetUser,
                    connection,
                    transaction))
            {
                insertarUsuarioIdentity.Parameters.Add(
                    "@Id",
                    SqlDbType.NVarChar,
                    128).Value = userId;

                insertarUsuarioIdentity.Parameters.Add(
                    "@Correo",
                    SqlDbType.NVarChar,
                    256).Value = Correo;

                insertarUsuarioIdentity.Parameters.Add(
                    "@PasswordHash",
                    SqlDbType.NVarChar,
                    -1).Value = passwordHash;

                insertarUsuarioIdentity.Parameters.Add(
                    "@SecurityStamp",
                    SqlDbType.NVarChar,
                    -1).Value = securityStamp;

                insertarUsuarioIdentity.Parameters.Add(
                    "@Telefono",
                    SqlDbType.NVarChar,
                    -1).Value = Telefono;

                insertarUsuarioIdentity.ExecuteNonQuery();
            }

            // -------------------------------------------------
            // 10. Registrar información personal
            // -------------------------------------------------

            const string insertarUsuario = @"
                INSERT INTO USUARIO
                (
                    IdNetUser,
                    Nombres,
                    PrimerApellido,
                    SegundoApellido,
                    Identificacion,
                    CorreoElectronico,
                    Telefono,
                    FechaDeRegistro,
                    FechaDeModificacion,
                    Estado
                )
                VALUES
                (
                    @IdNetUser,
                    @Nombres,
                    @PrimerApellido,
                    @SegundoApellido,
                    @Identificacion,
                    @Correo,
                    @Telefono,
                    GETDATE(),
                    NULL,
                    1
                );";

            using (SqlCommand insertarDatosUsuario =
                new SqlCommand(
                    insertarUsuario,
                    connection,
                    transaction))
            {
                insertarDatosUsuario.Parameters.Add(
                    "@IdNetUser",
                    SqlDbType.NVarChar,
                    128).Value = userId;

                insertarDatosUsuario.Parameters.Add(
                    "@Nombres",
                    SqlDbType.VarChar,
                    100).Value = Nombres;

                insertarDatosUsuario.Parameters.Add(
                    "@PrimerApellido",
                    SqlDbType.VarChar,
                    100).Value = PrimerApellido;

                insertarDatosUsuario.Parameters.Add(
                    "@SegundoApellido",
                    SqlDbType.VarChar,
                    100).Value = SegundoApellido;

                insertarDatosUsuario.Parameters.Add(
                    "@Identificacion",
                    SqlDbType.VarChar,
                    20).Value = Identificacion;

                insertarDatosUsuario.Parameters.Add(
                    "@Correo",
                    SqlDbType.VarChar,
                    200).Value = Correo;

                insertarDatosUsuario.Parameters.Add(
                    "@Telefono",
                    SqlDbType.VarChar,
                    20).Value = Telefono;

                insertarDatosUsuario.ExecuteNonQuery();
            }

            // -------------------------------------------------
            // 11. Asignar rol Administrador
            // -------------------------------------------------

            const string insertarRol = @"
                INSERT INTO AspNetUserRoles
                (
                    UserId,
                    RoleId
                )
                VALUES
                (
                    @UserId,
                    'ROL-ADMIN'
                );";

            using (SqlCommand asignarRol =
                new SqlCommand(
                    insertarRol,
                    connection,
                    transaction))
            {
                asignarRol.Parameters.Add(
                    "@UserId",
                    SqlDbType.NVarChar,
                    128).Value = userId;

                asignarRol.ExecuteNonQuery();
            }

            // -------------------------------------------------
            // 12. Confirmar operación
            // -------------------------------------------------

            transaction.Commit();

            TempData["RegistroAdminExitoso"] =
                "El administrador fue registrado correctamente.";

            return RedirectToAction(nameof(Index));
        }
        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch
            {
            }

            ModelState.AddModelError(
                string.Empty,
                "Ocurrió un error al registrar el administrador.");

            return View();
        }
    }
}