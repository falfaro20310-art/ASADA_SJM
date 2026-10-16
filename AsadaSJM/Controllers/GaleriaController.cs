using AsadaSJM.Models;
using AsadaSJM.Services.Galeria;
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

        public async Task<IActionResult> Index()
        {
            var resultado = await _galeriaService.ListarGaleriasAsync();

            if (!resultado.EsValido)
            {
                TempData["Error"] = resultado.MensajeValidacion;
                return View(new List<GaleriaDTO>());
            }

            return View(resultado.Datos);
        }

        // GET: GaleriaController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: GaleriaController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: GaleriaController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: GaleriaController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: GaleriaController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: GaleriaController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: GaleriaController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
