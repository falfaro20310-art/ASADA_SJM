using AsadaSJM.Models;
using AsadaSJM.Models.ViewModels;
using AsadaSJM.Services.Firebase;
using AsadaSJM.Services.FotoGaleria;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AsadaSJM.Controllers
{
    public class FotoGaleriaController : Controller
    {
        private readonly IFotoGaleriaService _fotoGaleriaService;

        public FotoGaleriaController(IFotoGaleriaService fotoGaleriaService)
        {
            _fotoGaleriaService = fotoGaleriaService;
        }

        public async Task<IActionResult> ObtenerFotosPorGaleriaAsync(int IdGaleria)
        {
            var resultado = await _fotoGaleriaService.ObtenerFotosPorGaleriaAsync(IdGaleria);

            if (resultado.EsValido)
            {
                var viewModel = new GaleriaFotosViewModel
                {
                    IdGaleria = IdGaleria,
                    Fotos = resultado.Datos ?? new List<FotoGaleriaDTO>()
                };

                return PartialView("_FotoGaleriaPartial", viewModel);
            }

            return BadRequest(resultado.MensajeValidacion);
        }

        [HttpPost]
        public async Task<ActionResult> Guardar(int idGaleria, IFormFile archivoImagen)
        {
            var modelo = new FotoGaleriaDTO
            {
                IdGaleria = idGaleria
            };

            var resultado = await _fotoGaleriaService.GuardarAsync(modelo, archivoImagen);

            if (resultado.EsValido)
            {
                return RedirectToAction("Index", "Galeria", new { id = modelo.IdGaleria });
            }

            TempData["Error"] = resultado.MensajeValidacion;
            return RedirectToAction("Index", "Galeria", new { id = modelo.IdGaleria });
        }

        public async Task<IActionResult> Eliminar(int idFoto)
        {
            var resultado = await _fotoGaleriaService.EliminarAsync(idFoto);

            if (resultado.EsValido)
            {
                TempData["MensajeExito"] = resultado.MensajeValidacion;
            }
            else
            {
                TempData["MensajeError"] = resultado.MensajeValidacion;
            }

            return RedirectToAction("Index");
        }
    }
}
