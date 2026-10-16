using AsadaSJM.Models;
using AsadaSJM.Models.General;

namespace AsadaSJM.Services.Galeria
{
    public interface IGaleriaService
    {
        Task<ValidacionDTO<List<GaleriaDTO>>> ListarGaleriasAsync(bool soloActivas = true);
        Task<ValidacionDTO<GaleriaDTO>> ObtenerGaleriaPorIdAsync(int id, bool soloActivas = true);
        Task<ValidacionDTO<GaleriaDTO>> CrearAsync(GaleriaDTO galeriaParaGuardar);
        Task<ValidacionDTO<GaleriaDTO>> EditarAsync(GaleriaDTO galeriaParaEditar);
    }
}
