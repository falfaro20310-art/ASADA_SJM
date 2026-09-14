using AsadaSJM.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsadaSJM.Controllers;

public class TramitesController : Controller
{
    private readonly ApplicationDbContext _context;
    public TramitesController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var tramites = await _context.Tramites
            .Include(t => t.Abonado)
            .OrderByDescending(t => t.FechaSolicitud)
            .ToListAsync();
        return View(tramites);
    }
}
