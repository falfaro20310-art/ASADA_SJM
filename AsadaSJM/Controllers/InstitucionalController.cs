using AsadaSJM.Data;
using AsadaSJM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace AsadaSJM.Controllers;

public class InstitucionalController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public InstitucionalController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    private Task<InformacionInstitucional?> ObtenerInformacionAsync()
    {
        return _context.InformacionInstitucional
            .AsNoTracking()
            .OrderByDescending(i => i.IdInformacion)
            .FirstOrDefaultAsync();
    }

    // GET: Institucional/Historia
    public async Task<IActionResult> Historia()
    {
        var info = await ObtenerInformacionAsync();

        ViewData["Titulo"] = "Historia";
        ViewData["Contenido"] = info?.Historia;

        return View("Detalle");
    }

    // GET: Institucional/Mision
    public async Task<IActionResult> Mision()
    {
        var info = await ObtenerInformacionAsync();

        ViewData["Titulo"] = "Misión";
        ViewData["Contenido"] = info?.Mision;

        return View("Detalle");
    }

    // GET: Institucional/Vision
    public async Task<IActionResult> Vision()
    {
        var info = await ObtenerInformacionAsync();

        ViewData["Titulo"] = "Visión";
        ViewData["Contenido"] = info?.Vision;

        return View("Detalle");
    }

    // GET: Institucional/Editar
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Editar()
    {
        var info = await ObtenerInformacionAsync();

        return View(info ?? new InformacionInstitucional());
    }

    // POST: Institucional/Editar
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Editar(InformacionInstitucional modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var entidad = modelo.IdInformacion != 0
            ? await _context.InformacionInstitucional.FindAsync(modelo.IdInformacion)
            : null;

        // Identifica al usurio en rol administrador que está realizando la actualización de la información institucional

        string? correoAdmin = User.FindFirstValue(ClaimTypes.Email);

        int? idUsuarioAdmin = null;

        using (var connection = new Microsoft.Data.SqlClient.SqlConnection(
            _configuration.GetConnectionString("DefaultConnection")))
        {
            using var command = new Microsoft.Data.SqlClient.SqlCommand(@"
                SELECT US.IdUsuario
                FROM USUARIO US
                INNER JOIN AspNetUsers AU ON AU.Id = US.IdNetUser
                WHERE AU.Email = @Correo", connection);

            command.Parameters.AddWithValue("@Correo", correoAdmin ?? (object)DBNull.Value);

            await connection.OpenAsync();
            var resultado = await command.ExecuteScalarAsync();

            if (resultado != null && resultado != DBNull.Value)
            {
                idUsuarioAdmin = Convert.ToInt32(resultado);
            }
        }

        if (entidad is null)
        {
            entidad = new InformacionInstitucional
            {
                FechaRegistro = DateTime.Now
            };

            _context.Add(entidad);
        }
        else
        {
            entidad.FechaModificacion = DateTime.Now;
        }

        entidad.IdUsuarioModificacion = idUsuarioAdmin;

        entidad.Historia = modelo.Historia;
        entidad.Mision = modelo.Mision;
        entidad.Vision = modelo.Vision;
        entidad.Correo = modelo.Correo;
        entidad.Telefono = modelo.Telefono;
        entidad.Direccion = modelo.Direccion;
        entidad.Horario = modelo.Horario;
        entidad.Facebook = modelo.Facebook;
        entidad.Instagram = modelo.Instagram;
        entidad.WhatsApp = modelo.WhatsApp;
        entidad.TikTok = modelo.TikTok;

        // Se registra en la bitácora la acción de actualización de la información institucional

        if (idUsuarioAdmin != null)
        {
            _context.Bitacoras.Add(new BitacoraItemViewModel
            {
                IdUsuario = idUsuarioAdmin.Value,
                Accion = "Actualizó la información institucional",
                FechaHora = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();

        TempData["GuardadoExitoso"] = "La información institucional fue actualizada correctamente.";

        return RedirectToAction(nameof(Editar));
    }
}