
using AsadaSJM.Data;
using AsadaSJM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsadaSJM.Controllers;

public class ComunidadAmbientalController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public ComunidadAmbientalController(
        ApplicationDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // =====================================================
    // MGA-02 A MGA-05: PÁGINA PÚBLICA
    // =====================================================

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        await CargarPaginaAsync();
        return View();
    }

    private async Task CargarPaginaAsync()
    {
        ViewBag.Campanas = await _context.CampanasAmbientales
            .AsNoTracking()
            .OrderByDescending(c => c.Fecha)
            .ToListAsync();

        ViewBag.FotosAmbientales =
            await _context.FotosProyectosAmbientales
                .AsNoTracking()
                .OrderByDescending(f => f.FechaRegistro)
                .ToListAsync();

        var carpetaMateriales = Path.Combine(
            _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot"),
            "materiales-ambientales");

        ViewBag.Materiales = Directory.Exists(carpetaMateriales)
            ? Directory.GetFiles(carpetaMateriales)
                .Where(r => string.Equals(
                    Path.GetExtension(r),
                    ".pdf",
                    StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetFileName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n!)
                .OrderBy(n => n)
                .ToList()
            : new List<string>();
    }

    // =====================================================
    // MGA-02: DESCARGAR MATERIAL EDUCATIVO
    // =====================================================

    [AllowAnonymous]
    [HttpGet]
    public IActionResult DescargarMaterial(string archivo)
    {
        if (string.IsNullOrWhiteSpace(archivo)
            || archivo != Path.GetFileName(archivo)
            || !string.Equals(
                Path.GetExtension(archivo),
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            TempData["AvisoAmbiental"] =
                "El material solicitado no está disponible.";

            return RedirectToAction(nameof(Index));
        }

        var ruta = Path.Combine(
            _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot"),
            "materiales-ambientales",
            archivo);

        if (!System.IO.File.Exists(ruta))
        {
            TempData["AvisoAmbiental"] =
                "No hay material disponible para descargar.";

            return RedirectToAction(nameof(Index));
        }

        return PhysicalFile(
            ruta,
            "application/pdf",
            archivo);
    }

    // =====================================================
    // MGA-03: REGISTRAR CAMPAÑA
    // =====================================================

    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public IActionResult CrearCampana()
    {
        return View(new CampanaAmbiental
        {
            Fecha = DateTime.Today
        });
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearCampana(
        [Bind("Titulo,Descripcion,Fecha,Lugar")]
        CampanaAmbiental campana,
        IFormFile? imagen)
    {
        if (campana.Fecha == default)
        {
            ModelState.AddModelError(
                nameof(campana.Fecha),
                "La fecha es obligatoria.");
        }

        ValidarImagen(imagen, "imagen");

        if (!ModelState.IsValid)
        {
            return View(campana);
        }

        campana.ImagenUrl =
            await GuardarImagenAsync(imagen!);

        campana.FechaRegistro = DateTime.Now;

        _context.CampanasAmbientales.Add(campana);

        await _context.SaveChangesAsync();

        TempData["ExitoAmbiental"] =
            "La campaña ambiental fue registrada correctamente.";

        return RedirectToAction(nameof(Index));
    }

    // =====================================================
    // MGA-04: INSCRIPCIÓN DE VOLUNTARIADO
    // =====================================================

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Inscribirse(
        [Bind("Nombre,Correo,Telefono,IdCampana")]
        InscripcionVoluntariado inscripcion)
    {
        if (inscripcion.IdCampana.HasValue &&
            !await _context.CampanasAmbientales.AnyAsync(
                c => c.IdCampana == inscripcion.IdCampana.Value))
        {
            ModelState.AddModelError(
                nameof(inscripcion.IdCampana),
                "Selecciona una campaña válida.");
        }

        if (!ModelState.IsValid)
        {
            await CargarPaginaAsync();

            ViewBag.Inscripcion = inscripcion;

            return View("Index");
        }

        inscripcion.FechaRegistro = DateTime.Now;

        _context.InscripcionesVoluntariado.Add(inscripcion);

        await _context.SaveChangesAsync();

        TempData["ExitoAmbiental"] =
            "Tu inscripción de voluntariado fue registrada correctamente.";

        return RedirectToAction(nameof(Index));
    }

    // =====================================================
    // MGA-05: PUBLICAR FOTOGRAFÍA
    // =====================================================

    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public IActionResult PublicarFoto()
    {
        return View(new FotoProyectoAmbiental());
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublicarFoto(
        [Bind("Titulo")]
        FotoProyectoAmbiental foto,
        IFormFile? imagen)
    {
        ValidarImagen(imagen, "imagen");

        if (!ModelState.IsValid)
        {
            return View(foto);
        }

        foto.ImagenUrl =
            await GuardarImagenAsync(imagen!);

        foto.FechaRegistro = DateTime.Now;

        _context.FotosProyectosAmbientales.Add(foto);

        await _context.SaveChangesAsync();

        TempData["ExitoAmbiental"] =
            "La fotografía fue publicada correctamente.";

        return RedirectToAction(nameof(Index));
    }

    // =====================================================
    // VALIDACIÓN DE IMÁGENES
    // =====================================================

    private void ValidarImagen(
        IFormFile? imagen,
        string campo)
    {
        if (imagen == null || imagen.Length == 0)
        {
            ModelState.AddModelError(
                campo,
                "Selecciona una imagen representativa.");

            return;
        }

        var extension = Path.GetExtension(
            imagen.FileName).ToLowerInvariant();

        if (extension != ".jpg"
            && extension != ".jpeg"
            && extension != ".png"
            && extension != ".webp")
        {
            ModelState.AddModelError(
                campo,
                "Solo se admiten imágenes JPG, PNG o WEBP.");
        }

        if (imagen.Length > 5 * 1024 * 1024)
        {
            ModelState.AddModelError(
                campo,
                "La imagen no puede superar los 5 MB.");
        }
    }

    // =====================================================
    // GUARDAR IMAGEN EN WWWROOT
    // =====================================================

    private async Task<string> GuardarImagenAsync(
        IFormFile imagen)
    {
        var carpeta = Path.Combine(
            _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot"),
            "img",
            "ambiental");

        Directory.CreateDirectory(carpeta);

        var nombre =
            $"{Guid.NewGuid():N}" +
            Path.GetExtension(
                imagen.FileName).ToLowerInvariant();

        var ruta = Path.Combine(carpeta, nombre);

        await using var destino = new FileStream(
            ruta,
            FileMode.CreateNew);

        await imagen.CopyToAsync(destino);

        return "/img/ambiental/" + nombre;
    }

    // =====================================================
    // NUEVO: PANEL DE ADMINISTRACIÓN AMBIENTAL
    // =====================================================

    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public async Task<IActionResult> Administrar()
    {
        ViewBag.Materiales = ObtenerMateriales();

        ViewBag.CampanasAdmin =
            await _context.CampanasAmbientales
                .AsNoTracking()
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();

        ViewBag.InscripcionesAdmin =
            await _context.InscripcionesVoluntariado
                .AsNoTracking()
                .OrderByDescending(i => i.FechaRegistro)
                .ToListAsync();

        ViewBag.FotosAdmin =
            await _context.FotosProyectosAmbientales
                .AsNoTracking()
                .OrderByDescending(f => f.FechaRegistro)
                .ToListAsync();

        return View();
    }

    // =====================================================
    // MGA-02: SUBIR PDF
    // =====================================================

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubirMaterial(IFormFile? archivo)
    {
        if (archivo == null || archivo.Length == 0)
        {
            TempData["AvisoAmbiental"] =
                "Selecciona un archivo PDF.";

            return RedirectToAction(nameof(Administrar));
        }

        if (archivo.Length > 15 * 1024 * 1024 ||
            !string.Equals(
                Path.GetExtension(archivo.FileName),
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            TempData["AvisoAmbiental"] =
                "El archivo debe ser PDF y no superar los 15 MB.";

            return RedirectToAction(nameof(Administrar));
        }

        await using (var origen = archivo.OpenReadStream())
        {
            var firma = new byte[5];

            if (await origen.ReadAsync(
                    firma, 0, firma.Length) != firma.Length ||
                System.Text.Encoding.ASCII.GetString(firma) != "%PDF-")
            {
                TempData["AvisoAmbiental"] =
                    "El documento seleccionado no es un PDF válido.";

                return RedirectToAction(nameof(Administrar));
            }
        }

        var baseNombre = Path.GetFileNameWithoutExtension(
            Path.GetFileName(archivo.FileName));

        var nombreSeguro = new string(baseNombre
            .Select(c =>
                char.IsLetterOrDigit(c) ||
                c == '-' ||
                c == '_' ? c : '_')
            .ToArray())
            .Trim('_');

        if (nombreSeguro.Length > 100)
        {
            nombreSeguro = nombreSeguro[..100];
        }

        if (string.IsNullOrWhiteSpace(nombreSeguro))
        {
            nombreSeguro = "material_educativo";
        }

        var nombre = nombreSeguro + ".pdf";
        var carpeta = CarpetaMateriales();

        Directory.CreateDirectory(carpeta);

        var ruta = Path.Combine(carpeta, nombre);

        if (System.IO.File.Exists(ruta))
        {
            TempData["AvisoAmbiental"] =
                "Ya existe un PDF con ese nombre. Renómbralo antes de subirlo.";

            return RedirectToAction(nameof(Administrar));
        }

        try
        {
            await using var destino =
                new FileStream(ruta, FileMode.CreateNew);

            await archivo.CopyToAsync(destino);
        }
        catch (IOException)
        {
            TempData["AvisoAmbiental"] =
                "No se pudo guardar el PDF. Comprueba el nombre e inténtalo de nuevo.";

            return RedirectToAction(nameof(Administrar));
        }

        TempData["ExitoAmbiental"] =
            "Material educativo publicado correctamente.";

        return RedirectToAction(nameof(Administrar));
    }

    // =====================================================
    // MGA-02: ELIMINAR PDF
    // =====================================================

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EliminarMaterial(string archivo)
    {
        if (!NombrePdfValido(archivo))
        {
            return BadRequest();
        }

        var ruta = Path.Combine(
            CarpetaMateriales(),
            archivo);

        if (!System.IO.File.Exists(ruta))
        {
            TempData["AvisoAmbiental"] =
                "El documento ya no existe.";
        }
        else
        {
            System.IO.File.Delete(ruta);

            TempData["ExitoAmbiental"] =
                "Material educativo eliminado.";
        }

        return RedirectToAction(nameof(Administrar));
    }

    // =====================================================
    // MGA-03: EDITAR CAMPAÑA
    // =====================================================

    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public async Task<IActionResult> EditarCampana(int id)
    {
        var campana =
            await _context.CampanasAmbientales.FindAsync(id);

        if (campana == null)
        {
            return NotFound();
        }

        return View(campana);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarCampana(
        int id,
        [Bind("IdCampana,Titulo,Descripcion,Fecha,Lugar")]
        CampanaAmbiental modelo,
        IFormFile? imagen)
    {
        if (id != modelo.IdCampana)
        {
            return NotFound();
        }

        if (modelo.Fecha == default)
        {
            ModelState.AddModelError(
                nameof(modelo.Fecha),
                "La fecha es obligatoria.");
        }

        if (imagen != null)
        {
            ValidarImagen(imagen, "imagen");
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var actual =
            await _context.CampanasAmbientales.FindAsync(id);

        if (actual == null)
        {
            return NotFound();
        }

        actual.Titulo = modelo.Titulo;
        actual.Descripcion = modelo.Descripcion;
        actual.Fecha = modelo.Fecha;
        actual.Lugar = modelo.Lugar;

        var imagenAnterior = actual.ImagenUrl;

        if (imagen != null)
        {
            actual.ImagenUrl =
                await GuardarImagenAsync(imagen);
        }

        await _context.SaveChangesAsync();

        if (imagen != null)
        {
            EliminarImagenLocal(imagenAnterior);
        }

        TempData["ExitoAmbiental"] =
            "Campaña actualizada correctamente.";

        return RedirectToAction(nameof(Administrar));
    }

    // =====================================================
    // MGA-03: ELIMINAR CAMPAÑA
    // =====================================================

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarCampana(int id)
    {
        var campana =
            await _context.CampanasAmbientales.FindAsync(id);

        if (campana == null)
        {
            return NotFound();
        }

        // Las inscripciones históricas se mantienen.
        // Solo se desvinculan de la campaña eliminada.

        await using var transaccion =
            await _context.Database.BeginTransactionAsync();

        await _context.InscripcionesVoluntariado
            .Where(i => i.IdCampana == id)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(
                    i => i.IdCampana,
                    (int?)null));

        _context.CampanasAmbientales.Remove(campana);

        await _context.SaveChangesAsync();

        await transaccion.CommitAsync();

        EliminarImagenLocal(campana.ImagenUrl);

        TempData["ExitoAmbiental"] =
            "Campaña eliminada. Las inscripciones históricas se conservaron.";

        return RedirectToAction(nameof(Administrar));
    }

    // =====================================================
    // MGA-05: ELIMINAR FOTOGRAFÍA AMBIENTAL
    // =====================================================

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarFoto(int id)
    {
        var foto =
            await _context.FotosProyectosAmbientales.FindAsync(id);

        if (foto == null)
        {
            return NotFound();
        }

        _context.FotosProyectosAmbientales.Remove(foto);

        await _context.SaveChangesAsync();

        EliminarImagenLocal(foto.ImagenUrl);

        TempData["ExitoAmbiental"] =
            "Fotografía ambiental eliminada.";

        return RedirectToAction(nameof(Administrar));
    }

    // =====================================================
    // AYUDAS PARA DOCUMENTOS PDF
    // =====================================================

    private string CarpetaMateriales()
    {
        return Path.Combine(
            _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot"),
            "materiales-ambientales");
    }

    private List<string> ObtenerMateriales()
    {
        if (!Directory.Exists(CarpetaMateriales()))
        {
            return new List<string>();
        }

        return Directory.GetFiles(CarpetaMateriales())
            .Where(r => string.Equals(
                Path.GetExtension(r),
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFileName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .OrderBy(n => n)
            .ToList();
    }

    private static bool NombrePdfValido(string? archivo)
    {
        return !string.IsNullOrWhiteSpace(archivo)
            && archivo == Path.GetFileName(archivo)
            && !archivo.Contains('\\')
            && string.Equals(
                Path.GetExtension(archivo),
                ".pdf",
                StringComparison.OrdinalIgnoreCase);
    }

    // =====================================================
    // ELIMINAR IMAGEN FÍSICA DE ESTE MÓDULO
    // =====================================================

    private void EliminarImagenLocal(string? url)
    {
        const string prefijo = "/img/ambiental/";

        if (string.IsNullOrWhiteSpace(url) ||
            !url.StartsWith(
                prefijo,
                StringComparison.Ordinal))
        {
            return;
        }

        var nombre = url[prefijo.Length..];

        if (nombre != Path.GetFileName(nombre) ||
            nombre.Contains('\\'))
        {
            return;
        }

        var ruta = Path.Combine(
            _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot"),
            "img",
            "ambiental",
            nombre);

        try
        {
            if (System.IO.File.Exists(ruta))
            {
                System.IO.File.Delete(ruta);
            }
        }
        catch (IOException)
        {
            // El registro de SQL ya se actualizó.
        }
        catch (UnauthorizedAccessException)
        {
            // No afecta al guardado de la base de datos.
        }
    }
}
