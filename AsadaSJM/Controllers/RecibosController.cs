using AsadaSJM.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsadaSJM.Controllers;

[Authorize(Roles = "Administrador,Operativo")]
public class RecibosController : Controller
{
    private readonly ApplicationDbContext _context;

    public RecibosController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var recibos = await _context.Recibos
            .Include(r => r.Abonado)
            .OrderByDescending(r => r.FechaEmision)
            .ToListAsync();

        return View(recibos);
    }
}