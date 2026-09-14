using AsadaSJM.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsadaSJM.Controllers;

public class DocumentosController : Controller
{
    private readonly ApplicationDbContext _context;
    public DocumentosController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var documentos = await _context.Documentos
            .Include(d => d.Tramite)
            .OrderByDescending(d => d.FechaCarga)
            .ToListAsync();
        return View(documentos);
    }
}
