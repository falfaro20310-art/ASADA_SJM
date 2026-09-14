using AsadaSJM.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsadaSJM.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    public HomeController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        ViewBag.AbonadosActivos = await _context.Abonados.CountAsync(a => a.Activo);
        ViewBag.AveriasProceso = await _context.Averias.CountAsync(a => a.Estado == "En proceso");
        ViewBag.TramitesPendientes = await _context.Tramites.CountAsync(t => t.Estado == "Pendiente" || t.Estado == "En revisión");

        var ultimasAverias = await _context.Averias
            .Include(a => a.Abonado)
            .OrderByDescending(a => a.FechaReporte)
            .Take(5)
            .ToListAsync();

        return View(ultimasAverias);
    }

    public IActionResult Error() => View();
}
