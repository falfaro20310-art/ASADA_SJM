namespace AsadaSJM.Services.Notificaciones;

public interface INotificacionTramiteService
{
    Task<ResultadoNotificacionTramite> NotificarCambioEstadoAsync(
        int idTramite,
        string estado,
        string? motivoRechazo = null);
}

public sealed class ResultadoNotificacionTramite
{
    public bool Exitoso { get; init; }
    public string Mensaje { get; init; } = string.Empty;
}
