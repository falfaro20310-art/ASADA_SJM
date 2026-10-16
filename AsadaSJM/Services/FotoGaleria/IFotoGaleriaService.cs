using AsadaSJM.Models;
using AsadaSJM.Models.General;

namespace AsadaSJM.Services.FotoGaleria
{
    public interface IFotoGaleriaService
    {
        Task<ValidacionDTO<List<FotoGaleriaDTO>>> ObtenerFotosPorGaleriaAsync(int IdGaleria);
        Task<ValidacionDTO<FotoGaleriaDTO>> GuardarAsync(FotoGaleriaDTO fotoParaGuardar);
        Task<ValidacionDTO<bool>> EliminarAsync(int IdFotoGaleria);
    }
}
