using AsadaSJM.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsadaSJM.Controllers;

public class UsuariosController : Controller
{
    private readonly ApplicationDbContext _context;
    public UsuariosController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var usuarios = await _context.Usuarios
            .Include(u => u.Rol)
            .OrderBy(u => u.Nombre)
            .ToListAsync();
        return View(usuarios);
    }
}
