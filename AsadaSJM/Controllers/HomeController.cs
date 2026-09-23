using AsadaSJM.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsadaSJM.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Cantidad de abonados registrados
        ViewBag.AbonadosActivos =
            await _context.Abonados.CountAsync();

        // Cantidad de averías registradas
        ViewBag.AveriasProceso =
            await _context.Averias.CountAsync();

        // Cantidad de trámites pendientes
        ViewBag.TramitesPendientes =
            await _context.Tramites.CountAsync(
                t => t.Estado == "Pendiente");

        // Últimas averías registradas
        var ultimasAverias = await _context.Averias
            .Include(a => a.Abonado)
            .OrderByDescending(a => a.FechaReporte)
            .Take(5)
            .ToListAsync();

        return View(ultimasAverias);
    }

    public IActionResult Error()
    {
        return View();
    }
}
