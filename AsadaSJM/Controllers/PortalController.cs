using AsadaSJM.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsadaSJM.Controllers;

public class PortalController : Controller
{
    private readonly ApplicationDbContext _context;

    public PortalController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var informacionContacto = await _context.InformacionInstitucional
            .AsNoTracking()
            .OrderByDescending(i => i.IdInformacion)
            .FirstOrDefaultAsync();

        ViewBag.InformacionContacto = informacionContacto;

        return View();
    }

    public async Task<IActionResult> Presidentes()
    {
        var presidenteActual = await _context.Expresidentes
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Estado);

        var expresidentes = await _context.Expresidentes
            .AsNoTracking()
            .Where(e => !e.Estado)
            .OrderByDescending(e => e.IdExpresidente)
            .ToListAsync();

        ViewBag.PresidenteActual = presidenteActual;

        return View(expresidentes);
    }
}