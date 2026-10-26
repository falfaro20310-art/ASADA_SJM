using AsadaSJM.Models;
using AsadaSJM.Services.Galeria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AsadaSJM.Controllers
{
    public class GaleriaController : Controller
    {
        private readonly IGaleriaService _galeriaService;

        public GaleriaController(IGaleriaService galeriaService)
        {
            _galeriaService = galeriaService;
        }

        public async Task<IActionResult> Index(int? id)
        {
            bool esAdministrador = User.IsInRole("Administrador");
            var resultado = await _galeriaService.ListarGaleriasAsync(soloActivas: !esAdministrador);

            if (!resultado.EsValido)
            {
                TempData["Error"] = resultado.MensajeValidacion;
                return View(new List<GaleriaDTO>());
            }

            return View(resultado.Datos);
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Crear()
        {
            return View("CrearGaleria", new GaleriaDTO { Estado = true, Fecha = DateTime.Now });
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(GaleriaDTO modelo)
        {
            var idUsuarioClaim = User.FindFirst("IdUsuario")?.Value;

            if (int.TryParse(idUsuarioClaim, out int idUsuario))
            {
                modelo.IdUsuario = idUsuario;
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View("CrearGaleria", modelo);
            }

            var resultado = await _galeriaService.CrearAsync(modelo);

            if (resultado.EsValido)
            {
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", resultado.MensajeValidacion);
            return View("CrearGaleria", modelo);
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Editar(int id)
        {
            var resultado = await _galeriaService.ObtenerGaleriaPorIdAsync(id);

            if (!resultado.EsValido || resultado.Datos == null)
            {
                TempData["Error"] = resultado.MensajeValidacion ?? "No se pudo cargar la galería.";
                return RedirectToAction("Index");
            }

            return View("EditarGaleria", resultado.Datos);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(GaleriaDTO modelo)
        {
            var idUsuarioClaim = User.FindFirst("IdUsuario")?.Value;
            if (int.TryParse(idUsuarioClaim, out int idUsuario))
            {
                modelo.IdUsuario = idUsuario;
            }

            if (!ModelState.IsValid)
            {
                return View("EditarGaleria", modelo);
            }

            var resultado = await _galeriaService.EditarAsync(modelo);

            if (resultado.EsValido)
            {
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", resultado.MensajeValidacion);
            return View("EditarGaleria", modelo);
        }
    }
}
