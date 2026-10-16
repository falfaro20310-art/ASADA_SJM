using AsadaSJM.Data;
using AsadaSJM.Data.Entidades;
using AsadaSJM.Models;
using AsadaSJM.Models.General;
using Microsoft.EntityFrameworkCore;

namespace AsadaSJM.Services.Galeria
{
    public class GaleriaService : IGaleriaService
    {
        private readonly ApplicationDbContext _context;

        public GaleriaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ValidacionDTO<List<GaleriaDTO>>> ListarGaleriasAsync(bool soloActivas = true)
        {
            try
            {
                IQueryable<GaleriaEntidad> query = _context.Galerias.AsNoTracking();

                if(soloActivas)
                {
                    query = query.Where(g => g.Estado);
                }

                List<GaleriaDTO> listaGalerias = await query.Select(g => new GaleriaDTO
                {
                    IdGaleria = g.IdGaleria,
                    IdUsuario = g.IdUsuario,
                    Titulo = g.Titulo,
                    Descripcion = g.Descripcion,
                    Fecha = g.Fecha,
                    Estado = g.Estado
                }).ToListAsync();

                return ValidacionDTO<List<GaleriaDTO>>.Exito(listaGalerias, "Galerías obtenidas correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ValidacionDTO<List<GaleriaDTO>>.Fallo("Ocurrió un error al procesar las galerías. Intente nuevamente más tarde.");
            }
        }

        public async Task<ValidacionDTO<GaleriaDTO>> ObtenerGaleriaPorIdAsync(int id, bool soloActivas = true)
        {
            try
            {
                IQueryable<GaleriaEntidad> query = _context.Galerias.AsNoTracking().Where(g => g.IdGaleria == id);

                if (soloActivas)
                {
                    query = query.Where(g => g.Estado);
                }

                GaleriaDTO? galeria = await query.Select(g => new GaleriaDTO
                    {
                        IdGaleria = g.IdGaleria,
                        IdUsuario = g.IdUsuario,
                        Titulo = g.Titulo,
                        Descripcion = g.Descripcion,
                        Fecha = g.Fecha,
                        Estado = g.Estado,
                        // Mapeamos las fotos directamente en la misma consulta PENDIENTE
                        Fotos = g.Fotos.Select(f => new FotoGaleriaDTO
                        {
                            IdGaleria = f.IdGaleria,
                            RutaImagen = f.RutaImagen,
                            Fecha = f.Fecha,
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (galeria == null)
                {
                    return ValidacionDTO<GaleriaDTO>.Fallo("La galería solicitada no existe o no está disponible.");
                }

                return ValidacionDTO<GaleriaDTO>.Exito(galeria, "Galería y fotos cargadas correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ValidacionDTO<GaleriaDTO>.Fallo("Error al cargar la galería.");
            }
        }

        public async Task<ValidacionDTO<GaleriaDTO>> CrearAsync(GaleriaDTO galeriaParaGuardar)
        {
            try
            {
                bool existeTitulo = await _context.Galerias.AnyAsync(g => g.Titulo.ToLower() == galeriaParaGuardar.Titulo.ToLower());
                if (existeTitulo)
                {
                    return ValidacionDTO<GaleriaDTO>.Fallo("Ya existe una galería con el mismo título.");
                }

                GaleriaEntidad nuevaGaleria = new GaleriaEntidad
                {
                    IdUsuario = galeriaParaGuardar.IdUsuario,
                    Titulo = galeriaParaGuardar.Titulo,
                    Descripcion = galeriaParaGuardar.Descripcion,
                    Fecha = galeriaParaGuardar.Fecha == default ? DateTime.Now : galeriaParaGuardar.Fecha,
                    Estado = galeriaParaGuardar.Estado
                };

                _context.Galerias.Add(nuevaGaleria);
                
                _context.Bitacoras.Add(new BitacoraItemViewModel
                    {
                        IdUsuario = galeriaParaGuardar.IdUsuario,
                        Accion = "Creó una nueva galería",
                        FechaHora = DateTime.Now
                    });

                await _context.SaveChangesAsync();

                galeriaParaGuardar.IdGaleria = nuevaGaleria.IdGaleria;

                return ValidacionDTO<GaleriaDTO>.Exito(galeriaParaGuardar, "Galería agregada correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ValidacionDTO<GaleriaDTO>.Fallo("Error al agregar la galería.");
            }
        }

        public async Task<ValidacionDTO<GaleriaDTO>> EditarAsync(GaleriaDTO galeriaParaEditar)
        {
            try
            {
                GaleriaEntidad? galeriaExistente = await _context.Galerias.FirstOrDefaultAsync(g => g.IdGaleria == galeriaParaEditar.IdGaleria);
                if (galeriaExistente == null)
                {
                    return ValidacionDTO<GaleriaDTO>.Fallo("No se encontró la galería a editar.");
                }

                galeriaExistente.IdUsuario = galeriaParaEditar.IdUsuario;
                galeriaExistente.Titulo = galeriaParaEditar.Titulo;
                galeriaExistente.Descripcion = galeriaParaEditar.Descripcion;
                galeriaExistente.Fecha = galeriaParaEditar.Fecha;
                galeriaExistente.Estado = galeriaParaEditar.Estado;

                _context.Galerias.Update(galeriaExistente);

                _context.Bitacoras.Add(new BitacoraItemViewModel
                {
                    IdUsuario = galeriaParaEditar.IdUsuario,
                    Accion = "Editó la galería: " + galeriaExistente.Titulo,
                    FechaHora = DateTime.Now
                });

                int cantidadDatosAfectados = await _context.SaveChangesAsync();

                return ValidacionDTO<GaleriaDTO>.Exito(galeriaParaEditar, "Galería editada correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ValidacionDTO<GaleriaDTO>.Fallo("Error al editar la galería.");
            }
        }
    }
}
