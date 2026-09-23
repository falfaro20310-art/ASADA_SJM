using AsadaSJM.Data;
using AsadaSJM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsadaSJM.Controllers;

public class AbonadosController : Controller
{
    private readonly ApplicationDbContext _context;

    public AbonadosController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Abonados
    public async Task<IActionResult> Index(string? buscar)
    {
        var query = _context.Abonados.AsQueryable();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            query = query.Where(a =>
                a.NIS.Contains(buscar) ||
                a.NumeroPaja.Contains(buscar) ||
                (a.NumeroFinca != null && a.NumeroFinca.Contains(buscar)) ||
                (a.NumeroPlano != null && a.NumeroPlano.Contains(buscar)) ||
                a.Direccion.Contains(buscar));
        }

        return View(
            await query
                .OrderBy(a => a.NIS)
                .ToListAsync()
        );
    }

    // GET: Abonados/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Abonados/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("IdUsuario,NIS,NumeroPaja,NumeroFinca,NumeroPlano,Direccion,IdTarifa,IdSector")]
        Abonado abonado)
    {
        if (!ModelState.IsValid)
        {
            return View(abonado);
        }

        _context.Add(abonado);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: Abonados/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var abonado = await _context.Abonados.FindAsync(id);

        if (abonado is null)
        {
            return NotFound();
        }

        return View(abonado);
    }

    // POST: Abonados/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("IdAbonado,IdUsuario,NIS,NumeroPaja,NumeroFinca,NumeroPlano,Direccion,IdTarifa,IdSector")]
        Abonado abonado)
    {
        if (id != abonado.IdAbonado)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(abonado);
        }

        _context.Update(abonado);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: Abonados/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var abonado = await _context.Abonados
            .FirstOrDefaultAsync(a => a.IdAbonado == id);

        if (abonado is null)
        {
            return NotFound();
        }

        return View(abonado);
    }

    // POST: Abonados/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var abonado = await _context.Abonados.FindAsync(id);

        if (abonado is not null)
        {
            _context.Abonados.Remove(abonado);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
