using AsadaSJM.Data;
using AsadaSJM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsadaSJM.Controllers;

[Authorize(Roles = "Administrador,Operativo")]
public class AveriasController : Controller
{
    private readonly ApplicationDbContext _context;

    public AveriasController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Averias
    public async Task<IActionResult> Index()
    {
        var averias = await _context.Averias
            .Include(a => a.Abonado)
            .OrderByDescending(a => a.FechaReporte)
            .ToListAsync();

        return View(averias);
    }

    // GET: Averias/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Averias/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Averia averia)
    {
        if (!ModelState.IsValid)
        {
            return View(averia);
        }

        _context.Averias.Add(averia);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: Averias/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var averia = await _context.Averias
            .Include(a => a.Abonado)
            .FirstOrDefaultAsync(a => a.IdAveria == id);

        if (averia is null)
        {
            return NotFound();
        }

        return View(averia);
    }

    // GET: Averias/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var averia = await _context.Averias
            .FirstOrDefaultAsync(a => a.IdAveria == id);

        if (averia is null)
        {
            return NotFound();
        }

        return View(averia);
    }

    // POST: Averias/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Averia averia)
    {
        if (id != averia.IdAveria)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(averia);
        }

        _context.Update(averia);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: Averias/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var averia = await _context.Averias
            .Include(a => a.Abonado)
            .FirstOrDefaultAsync(a => a.IdAveria == id);

        if (averia is null)
        {
            return NotFound();
        }

        return View(averia);
    }

    // POST: Averias/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var averia = await _context.Averias.FindAsync(id);

        if (averia is not null)
        {
            _context.Averias.Remove(averia);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}