
using AsadaSJM.Data;
using AsadaSJM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsadaSJM.Controllers;

public class GestionAmbientalController : Controller
{
    private readonly ApplicationDbContext _context;

    public GestionAmbientalController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // MGA-01: CONSULTA PUBLICA
    // ==========================================

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var nacientes = await _context.Nacientes
            .AsNoTracking()
            .Where(n => n.Estado)
            .OrderBy(n => n.Nombre)
            .ToListAsync();

        var tanques = await _context.Tanques
            .AsNoTracking()
            .Where(t => t.Estado)
            .OrderBy(t => t.Nombre)
            .ToListAsync();

        ViewBag.Nacientes = nacientes;
        ViewBag.Tanques = tanques;

        return View();
    }

    // ==========================================
    // MGA-01: ADMINISTRACION
    // ==========================================

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Administrar()
    {
        var nacientes = await _context.Nacientes
            .AsNoTracking()
            .OrderBy(n => n.Nombre)
            .ToListAsync();

        var tanques = await _context.Tanques
            .AsNoTracking()
            .OrderBy(t => t.Nombre)
            .ToListAsync();

        ViewBag.Nacientes = nacientes;
        ViewBag.Tanques = tanques;

        return View();
    }

    // ==========================================
    // MGA-01: CREAR NACIENTE
    // ==========================================

    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public IActionResult CrearNaciente()
    {
        return View(new Naciente());
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearNaciente(
        [Bind("Nombre,Ubicacion,Descripcion,Estado")]
        Naciente naciente)
    {
        if (!ModelState.IsValid)
        {
            return View(naciente);
        }

        naciente.FechaRegistro = DateTime.Now;

        _context.Nacientes.Add(naciente);
        await _context.SaveChangesAsync();

        TempData["Mensaje"] =
            "La naciente fue registrada correctamente.";

        return RedirectToAction(nameof(Administrar));
    }

    // ==========================================
    // MGA-01: EDITAR NACIENTE
    // ==========================================

    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public async Task<IActionResult> EditarNaciente(int id)
    {
        var naciente = await _context.Nacientes
            .FindAsync(id);

        if (naciente == null)
        {
            return NotFound();
        }

        return View(naciente);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarNaciente(
        int id,
        [Bind("IdNaciente,Nombre,Ubicacion,Descripcion,Estado")]
        Naciente modelo)
    {
        if (id != modelo.IdNaciente)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var naciente = await _context.Nacientes
            .FindAsync(id);

        if (naciente == null)
        {
            return NotFound();
        }

        naciente.Nombre = modelo.Nombre;
        naciente.Ubicacion = modelo.Ubicacion;
        naciente.Descripcion = modelo.Descripcion;
        naciente.Estado = modelo.Estado;

        await _context.SaveChangesAsync();

        TempData["Mensaje"] =
            "La naciente fue actualizada correctamente.";

        return RedirectToAction(nameof(Administrar));
    }

    // ==========================================
    // MGA-01: ELIMINAR NACIENTE
    // ==========================================

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarNaciente(int id)
    {
        var naciente = await _context.Nacientes
            .FindAsync(id);

        if (naciente == null)
        {
            return NotFound();
        }

        _context.Nacientes.Remove(naciente);
        await _context.SaveChangesAsync();

        TempData["Mensaje"] =
            "La naciente fue eliminada correctamente.";

        return RedirectToAction(nameof(Administrar));
    }

    // ==========================================
    // MGA-01: CREAR TANQUE
    // ==========================================

    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public IActionResult CrearTanque()
    {
        return View(new Tanque());
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearTanque(
        [Bind("Nombre,Ubicacion,Descripcion,Estado")]
        Tanque tanque)
    {
        if (!ModelState.IsValid)
        {
            return View(tanque);
        }

        tanque.FechaRegistro = DateTime.Now;

        _context.Tanques.Add(tanque);
        await _context.SaveChangesAsync();

        TempData["Mensaje"] =
            "El tanque fue registrado correctamente.";

        return RedirectToAction(nameof(Administrar));
    }

    // ==========================================
    // MGA-01: EDITAR TANQUE
    // ==========================================

    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public async Task<IActionResult> EditarTanque(int id)
    {
        var tanque = await _context.Tanques
            .FindAsync(id);

        if (tanque == null)
        {
            return NotFound();
        }

        return View(tanque);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarTanque(
        int id,
        [Bind("IdTanque,Nombre,Ubicacion,Descripcion,Estado")]
        Tanque modelo)
    {
        if (id != modelo.IdTanque)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var tanque = await _context.Tanques
            .FindAsync(id);

        if (tanque == null)
        {
            return NotFound();
        }

        tanque.Nombre = modelo.Nombre;
        tanque.Ubicacion = modelo.Ubicacion;
        tanque.Descripcion = modelo.Descripcion;
        tanque.Estado = modelo.Estado;

        await _context.SaveChangesAsync();

        TempData["Mensaje"] =
            "El tanque fue actualizado correctamente.";

        return RedirectToAction(nameof(Administrar));
    }

    // ==========================================
    // MGA-01: ELIMINAR TANQUE
    // ==========================================

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarTanque(int id)
    {
        var tanque = await _context.Tanques
            .FindAsync(id);

        if (tanque == null)
        {
            return NotFound();
        }

        _context.Tanques.Remove(tanque);
        await _context.SaveChangesAsync();

        TempData["Mensaje"] =
            "El tanque fue eliminado correctamente.";

        return RedirectToAction(nameof(Administrar));
    }
}
