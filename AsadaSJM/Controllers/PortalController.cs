
using AsadaSJM.Data;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AsadaSJM.Controllers;

public class PortalController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public PortalController(
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<IActionResult> Index()
    {
        var informacionContacto =
            await _context.InformacionInstitucional
                .AsNoTracking()
                .OrderByDescending(i => i.IdInformacion)
                .FirstOrDefaultAsync();

        ViewBag.InformacionContacto = informacionContacto;

        // MPA-02: mostrar únicamente noticias
        // publicadas y con vigencia válida.
        var noticias = new DataTable();

        var connectionString =
            _configuration.GetConnectionString(
                "DefaultConnection");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            await using var conexion =
                new SqlConnection(connectionString);

            await conexion.OpenAsync();

            await using var comando =
                new SqlCommand(@"
                    SELECT
                        Titulo,
                        Contenido,
                        RutaImagen,
                        FechaPublicacion
                    FROM dbo.MPA_NOTICIA
                    WHERE
                        Estado='Publicado'
                        AND FechaVigencia IS NOT NULL
                        AND CONVERT(date,FechaVigencia)
                            >= CONVERT(date,GETDATE())
                    ORDER BY FechaPublicacion DESC;",
                    conexion);

            await using var lector =
                await comando.ExecuteReaderAsync();

            noticias.Load(lector);
        }

        ViewBag.NoticiasPublicas = noticias;

        return View();
    }

    public async Task<IActionResult> Presidentes()
    {
        var presidenteActual =
            await _context.Expresidentes
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Estado);

        var expresidentes =
            await _context.Expresidentes
                .AsNoTracking()
                .Where(e => !e.Estado)
                .OrderByDescending(e => e.IdExpresidente)
                .ToListAsync();

        ViewBag.PresidenteActual = presidenteActual;

        return View(expresidentes);
    }
}
