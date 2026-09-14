using AsadaSJM.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsadaSJM.Controllers;

public class AveriasController : Controller
{
    private readonly ApplicationDbContext _context;
    public AveriasController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var averias = await _context.Averias
            .Include(a => a.Abonado)
            .Include(a => a.Responsable)
            .OrderByDescending(a => a.FechaReporte)
            .ToListAsync();
        return View(averias);
    }
}
