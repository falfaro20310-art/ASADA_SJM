using AsadaSJM.Data;
using AsadaSJM.Data.Entidades;
using AsadaSJM.Models;
using AsadaSJM.Models.General;
using AsadaSJM.Services.Firebase;
using Microsoft.EntityFrameworkCore;

namespace AsadaSJM.Services.FotoGaleria
{
    public class FotoGaleriaService : IFotoGaleriaService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFirebaseStorageService _firebaseStorageService;

        public FotoGaleriaService(ApplicationDbContext context, IFirebaseStorageService firebaseStorageService)
        {
            _context = context;
            _firebaseStorageService = firebaseStorageService;
        }

        public async Task<ValidacionDTO<List<FotoGaleriaDTO>>> ObtenerFotosPorGaleriaAsync(int IdGaleria)
        {
            try
            {
                IQueryable<FotoGaleriaEntidad> query = _context.FotosGaleria
                    .Where(f => f.IdGaleria == IdGaleria)
                    .AsNoTracking();

                List<FotoGaleriaDTO> listaFotos = await query.Select(f => new FotoGaleriaDTO
                {
                    IdFoto = f.IdFoto,
                    IdGaleria = f.IdGaleria,
                    RutaImagen = f.RutaImagen
                }).ToListAsync();

                return ValidacionDTO<List<FotoGaleriaDTO>>.Exito(listaFotos, "Fotos obtenidas correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ValidacionDTO<List<FotoGaleriaDTO>>.Fallo("Error al obtener las fotos.");
            }
        }

        public async Task<ValidacionDTO<FotoGaleriaDTO>> GuardarAsync(FotoGaleriaDTO fotoParaGuardar, IFormFile archivoImagen)
        {
            try
            {
                if (archivoImagen != null && archivoImagen.Length > 0)
                {
                    string urlFirebase = await _firebaseStorageService.UploadImageAsync(archivoImagen);
                    fotoParaGuardar.RutaImagen = urlFirebase;
                }
                else
                {
                    return ValidacionDTO<FotoGaleriaDTO>.Fallo("Debe adjuntar una imagen válida.");
                }

                FotoGaleriaEntidad nuevaFoto = new FotoGaleriaEntidad
                {
                    IdGaleria = fotoParaGuardar.IdGaleria,
                    RutaImagen = fotoParaGuardar.RutaImagen
                };

                _context.FotosGaleria.Add(nuevaFoto);
                await _context.SaveChangesAsync();

                fotoParaGuardar.IdFoto = nuevaFoto.IdFoto;

                return ValidacionDTO<FotoGaleriaDTO>.Exito(fotoParaGuardar, "Foto guardada correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ValidacionDTO<FotoGaleriaDTO>.Fallo("Error detallado: " + ex.Message + " | Inner: " + ex.InnerException?.Message);
            }
        }

        public async Task<ValidacionDTO<bool>> EliminarAsync(int idFoto)
        {
            try
            {
                var foto = await _context.FotosGaleria.FirstOrDefaultAsync(f => f.IdFoto == idFoto);
                if (foto == null)
                {
                    return ValidacionDTO<bool>.Fallo("La foto solicitada no existe o no está disponible.");
                }

                _context.FotosGaleria.Remove(foto);
                await _context.SaveChangesAsync();
                return ValidacionDTO<bool>.Exito(true, "Foto eliminada correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ValidacionDTO<bool>.Fallo("Error al eliminar la foto.");
            }
        }  
    }
}
