using AsadaSJM.Data;
using AsadaSJM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AsadaSJM.Controllers;

[Authorize(Roles = "Administrador")]
public class ExpresidentesController : Controller
{
    private readonly ApplicationDbContext _context;

    private readonly IConfiguration _configuration;

    public ExpresidentesController(
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // GET: Expresidentes
    public async Task<IActionResult> Index()
    {
        var presidentes = await _context.Expresidentes
            .AsNoTracking()
            .OrderByDescending(e => e.Estado)
            .ThenByDescending(e => e.IdExpresidente)
            .ToListAsync();

        return View(presidentes);
    }

    // GET: Expresidentes/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Expresidentes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Nombre,Trayectoria,Proyectos,Imagen,Estado")]
    Expresidente expresidente)
    {
        if (!ModelState.IsValid)
        {
            return View(expresidente);
        }

        // Indentifica al administrador que registra presidentes o expresidentes

        string? correoAdmin = User.FindFirstValue(System.Security.Claims.ClaimTypes.Email);

        using (var connection = new Microsoft.Data.SqlClient.SqlConnection(
            _configuration.GetConnectionString("DefaultConnection")))
        {
            using var command = new Microsoft.Data.SqlClient.SqlCommand(@"
            SELECT US.IdUsuario
            FROM USUARIO US
            INNER JOIN AspNetUsers AU ON AU.Id = US.IdNetUser
            WHERE AU.Email = @Correo", connection);

            command.Parameters.AddWithValue("@Correo", correoAdmin ?? (object)DBNull.Value);

            await connection.OpenAsync();
            var resultado = await command.ExecuteScalarAsync();

            if (resultado != null && resultado != DBNull.Value)
            {
                expresidente.IdUsuario = Convert.ToInt32(resultado);
            }
        }

        if (expresidente.Estado)
        {
            var presidentesActuales = await _context.Expresidentes
                .Where(e => e.Estado)
                .ToListAsync();

            foreach (var presidente in presidentesActuales)
            {
                presidente.Estado = false;
            }
        }

        _context.Add(expresidente);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: Expresidentes/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var expresidente = await _context.Expresidentes
            .FindAsync(id);

        if (expresidente is null)
        {
            return NotFound();
        }

        return View(expresidente);
    }

    // POST: Expresidentes/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("IdExpresidente,Nombre,Trayectoria,Proyectos,Imagen,Estado")]
        Expresidente expresidente)
    {
        if (id != expresidente.IdExpresidente)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(expresidente);
        }

        if (expresidente.Estado)
        {
            var presidentesActuales = await _context.Expresidentes
                .Where(e =>
                    e.Estado &&
                    e.IdExpresidente != expresidente.IdExpresidente)
                .ToListAsync();

            foreach (var presidente in presidentesActuales)
            {
                presidente.Estado = false;
            }
        }

        _context.Update(expresidente);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // POST: Expresidentes/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var expresidente = await _context.Expresidentes
            .FindAsync(id);

        if (expresidente is null)
        {
            return NotFound();
        }

        _context.Expresidentes.Remove(expresidente);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}