using AsadaSJM.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsadaSJM.Controllers;

public class RecibosController : Controller
{
    private readonly ApplicationDbContext _context;
    public RecibosController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var recibos = await _context.Recibos
            .Include(r => r.Abonado)
            .OrderByDescending(r => r.FechaEmision)
            .ToListAsync();
        return View(recibos);
    }
}
