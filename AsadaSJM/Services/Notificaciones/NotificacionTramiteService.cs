using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using MimeKit;
using System.Net;

namespace AsadaSJM.Services.Notificaciones;

public sealed class NotificacionTramiteService : INotificacionTramiteService
{
    private readonly IConfiguration _configuration;
    private readonly IDataProtector _protector;

    public NotificacionTramiteService(
        IConfiguration configuration,
        IDataProtectionProvider dataProtectionProvider)
    {
        _configuration = configuration;
        _protector = dataProtectionProvider.CreateProtector(
            "AsadaSJM.ConfiguracionCorreo.Password");
    }

    public async Task<ResultadoNotificacionTramite> NotificarCambioEstadoAsync(
        int idTramite,
        string estado,
        string? motivoRechazo = null)
    {
        string? connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new ResultadoNotificacionTramite
            {
                Exitoso = false,
                Mensaje = "No se encontró la cadena de conexión."
            };
        }

        DatosNotificacion? datos = null;
        ConfiguracionSmtp? smtpConfig = null;

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            datos = await CargarDatosTramiteAsync(
                connection,
                idTramite);

            if (datos is null)
            {
                return new ResultadoNotificacionTramite
                {
                    Exitoso = false,
                    Mensaje = "No se encontraron los datos del trámite."
                };
            }

            if (string.IsNullOrWhiteSpace(datos.Correo))
            {
                await RegistrarIntentoAsync(
                    connection,
                    idTramite,
                    "Email",
                    null,
                    "Fallida",
                    "El usuario no tiene correo registrado.");

                return new ResultadoNotificacionTramite
                {
                    Exitoso = false,
                    Mensaje = "El usuario no tiene correo registrado."
                };
            }

            smtpConfig = await CargarConfiguracionSmtpAsync(connection);

            if (smtpConfig is null)
            {
                await RegistrarIntentoAsync(
                    connection,
                    idTramite,
                    "Email",
                    datos.Correo,
                    "Fallida",
                    "No existe configuración SMTP.");

                return new ResultadoNotificacionTramite
                {
                    Exitoso = false,
                    Mensaje = "No existe configuración SMTP."
                };
            }

            var incidencias =
                await ObtenerIncidenciasDocumentalesAsync(
                    connection,
                    idTramite,
                    datos.EsPropietarioRegistral,
                    datos.EsSegregacionFraccionamiento);

            string password =
                _protector.Unprotect(
                    smtpConfig.PasswordEncriptado);

            var mensaje = ConstruirMensaje(
                datos,
                estado,
                motivoRechazo,
                incidencias,
                smtpConfig);

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(
                smtpConfig.Host,
                smtpConfig.Port,
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                smtpConfig.Username,
                password);

            await smtp.SendAsync(mensaje);
            await smtp.DisconnectAsync(true);

            await RegistrarIntentoAsync(
                connection,
                idTramite,
                "Email",
                datos.Correo,
                "Enviada",
                $"Notificación de estado {estado} enviada correctamente.");

            return new ResultadoNotificacionTramite
            {
                Exitoso = true,
                Mensaje = $"Correo enviado a {datos.Correo}."
            };
        }
        catch (Exception ex)
        {
            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                await RegistrarIntentoAsync(
                    connection,
                    idTramite,
                    "Email",
                    datos?.Correo,
                    "Fallida",
                    Recortar(ex.Message, 900));
            }
            catch
            {
                // La notificación nunca debe revertir la aprobación/rechazo.
            }

            return new ResultadoNotificacionTramite
            {
                Exitoso = false,
                Mensaje = $"El trámite se actualizó, pero no fue posible enviar el correo: {ex.Message}"
            };
        }
    }

    private static async Task<DatosNotificacion?> CargarDatosTramiteAsync(
        SqlConnection connection,
        int idTramite)
    {
        const string consulta = @"
            SELECT TOP 1
                s.NumeroTramite,
                s.NombreSolicitante,
                s.EsPropietarioRegistral,
                s.EsSegregacionFraccionamiento,
                anu.Email
            FROM TRAMITE t
            INNER JOIN SOLICITUD_DISPONIBILIDAD_HIDRICA s
                ON s.IdTramite = t.IdTramite
            INNER JOIN USUARIO u
                ON u.IdUsuario = t.IdUsuario
            LEFT JOIN AspNetUsers anu
                ON anu.Id = u.IdNetUser
            WHERE t.IdTramite = @IdTramite;";

        await using var command =
            new SqlCommand(consulta, connection);

        command.Parameters.AddWithValue(
            "@IdTramite",
            idTramite);

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new DatosNotificacion
        {
            NumeroTramite =
                reader["NumeroTramite"]?.ToString()
                ?? string.Empty,

            NombreSolicitante =
                reader["NombreSolicitante"]?.ToString()
                ?? string.Empty,

            Correo =
                reader["Email"]?.ToString()
                ?? string.Empty,

            EsPropietarioRegistral =
                Convert.ToBoolean(
                    reader["EsPropietarioRegistral"]),

            EsSegregacionFraccionamiento =
                Convert.ToBoolean(
                    reader["EsSegregacionFraccionamiento"])
        };
    }

    private static async Task<ConfiguracionSmtp?> CargarConfiguracionSmtpAsync(
        SqlConnection connection)
    {
        const string consulta = @"
            SELECT TOP 1
                Host,
                Port,
                Username,
                PasswordEncriptado,
                FromEmail,
                FromName
            FROM ConfiguracionCorreo
            WHERE Id = 1;";

        await using var command =
            new SqlCommand(consulta, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new ConfiguracionSmtp
        {
            Host =
                reader["Host"]?.ToString()
                ?? string.Empty,

            Port =
                Convert.ToInt32(
                    reader["Port"]),

            Username =
                reader["Username"]?.ToString()
                ?? string.Empty,

            PasswordEncriptado =
                reader["PasswordEncriptado"]?.ToString()
                ?? string.Empty,

            FromEmail =
                reader["FromEmail"]?.ToString()
                ?? string.Empty,

            FromName =
                reader["FromName"]?.ToString()
                ?? string.Empty
        };
    }

    private static async Task<List<string>> ObtenerIncidenciasDocumentalesAsync(
        SqlConnection connection,
        int idTramite,
        bool esPropietarioRegistral,
        bool esSegregacionFraccionamiento)
    {
        var documentos = new List<DocumentoTramite>();

        const string consulta = @"
            SELECT
                TipoDocumento,
                NombreArchivo,
                FechaEmision,
                VigenciaDias
            FROM TRAMITE_DOCUMENTO
            WHERE IdTramite = @IdTramite;";

        await using (var command =
            new SqlCommand(consulta, connection))
        {
            command.Parameters.AddWithValue(
                "@IdTramite",
                idTramite);

            await using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                documentos.Add(
                    new DocumentoTramite
                    {
                        TipoDocumento =
                            reader["TipoDocumento"]?.ToString()
                            ?? string.Empty,

                        NombreArchivo =
                            reader["NombreArchivo"]?.ToString()
                            ?? string.Empty,

                        FechaEmision =
                            reader["FechaEmision"] == DBNull.Value
                                ? null
                                : Convert.ToDateTime(
                                    reader["FechaEmision"]),

                        VigenciaDias =
                            reader["VigenciaDias"] == DBNull.Value
                                ? null
                                : Convert.ToInt32(
                                    reader["VigenciaDias"])
                    });
            }
        }

        var incidencias = new List<string>();

        if (!documentos.Any(
            d => d.TipoDocumento.Equals(
                "Identificacion",
                StringComparison.OrdinalIgnoreCase)))
        {
            incidencias.Add(
                "Falta el documento de identificación vigente.");
        }

        if (!documentos.Any(
            d => d.TipoDocumento.Equals(
                "CertificacionLiteral",
                StringComparison.OrdinalIgnoreCase)))
        {
            incidencias.Add(
                "Falta la certificación literal.");
        }

        if (!esPropietarioRegistral &&
            !documentos.Any(
                d => d.TipoDocumento.Equals(
                    "AutorizacionPropietario",
                    StringComparison.OrdinalIgnoreCase)))
        {
            incidencias.Add(
                "Falta la autorización o poder especial del propietario.");
        }

        if (esSegregacionFraccionamiento)
        {
            bool tienePlano =
                documentos.Any(
                    d => d.TipoDocumento.Equals(
                        "PlanoAgrimensuraFracciones",
                        StringComparison.OrdinalIgnoreCase));

            bool tieneMinuta =
                documentos.Any(
                    d => d.TipoDocumento.Equals(
                        "MinutaRechazo",
                        StringComparison.OrdinalIgnoreCase));

            if (!tienePlano && !tieneMinuta)
            {
                incidencias.Add(
                    "Falta el plano de agrimensura de las fracciones o la minuta de rechazo.");
            }
        }

        foreach (var documento in documentos)
        {
            if (!documento.FechaEmision.HasValue ||
                !documento.VigenciaDias.HasValue)
            {
                continue;
            }

            DateTime fechaVencimiento =
                documento.FechaEmision.Value.Date
                    .AddDays(
                        documento.VigenciaDias.Value);

            if (fechaVencimiento < DateTime.Today)
            {
                string nombre =
                    string.IsNullOrWhiteSpace(
                        documento.NombreArchivo)
                        ? documento.TipoDocumento
                        : documento.NombreArchivo;

                incidencias.Add(
                    $"{nombre} está vencido desde {fechaVencimiento:dd/MM/yyyy}.");
            }
        }

        return incidencias;
    }

    private static MimeMessage ConstruirMensaje(
        DatosNotificacion datos,
        string estado,
        string? motivoRechazo,
        IReadOnlyCollection<string> incidencias,
        ConfiguracionSmtp smtpConfig)
    {
        string estadoSeguro =
            WebUtility.HtmlEncode(estado);

        string numeroSeguro =
            WebUtility.HtmlEncode(
                datos.NumeroTramite);

        string nombreSeguro =
            WebUtility.HtmlEncode(
                datos.NombreSolicitante);

        var mensaje = new MimeMessage();

        mensaje.From.Add(
            new MailboxAddress(
                smtpConfig.FromName,
                smtpConfig.FromEmail));

        mensaje.To.Add(
            MailboxAddress.Parse(
                datos.Correo));

        mensaje.Subject =
            $"Actualización de trámite {datos.NumeroTramite} - {estado}";

        string detalleEstado;

        if (estado.Equals(
            "Aprobado",
            StringComparison.OrdinalIgnoreCase))
        {
            detalleEstado = @"
                <p>
                    Su solicitud fue <strong>aprobada</strong>.
                    La constancia de Disponibilidad Hídrica
                    ya fue generada por el sistema.
                </p>";
        }
        else
        {
            string motivoSeguro =
                WebUtility.HtmlEncode(
                    string.IsNullOrWhiteSpace(
                        motivoRechazo)
                        ? "No indicado."
                        : motivoRechazo);

            detalleEstado = $@"
                <p>
                    Su solicitud fue <strong>rechazada</strong>.
                </p>

                <p>
                    <strong>Motivo:</strong>
                    {motivoSeguro}
                </p>";
        }

        string incidenciasHtml = string.Empty;

        if (incidencias.Count > 0)
        {
            string items =
                string.Join(
                    Environment.NewLine,
                    incidencias.Select(
                        x => $"<li>{WebUtility.HtmlEncode(x)}</li>"));

            incidenciasHtml = $@"
                <p>
                    <strong>Documentos faltantes o vencidos:</strong>
                </p>
                <ul>
                    {items}
                </ul>";
        }

        mensaje.Body =
            new TextPart("html")
            {
                Text = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; color: #222;'>

                        <h2>
                            ASADA San José de la Montaña
                        </h2>

                        <p>
                            Estimado(a) {nombreSeguro}:
                        </p>

                        <p>
                            El trámite
                            <strong>{numeroSeguro}</strong>
                            cambió al estado
                            <strong>{estadoSeguro}</strong>.
                        </p>

                        {detalleEstado}

                        {incidenciasHtml}

                        <p>
                            Puede consultar el estado de sus trámites
                            desde la Oficina Virtual.
                        </p>

                        <p style='font-size: 12px; color: #666;'>
                            Este es un mensaje automático del sistema ASADA SJM.
                        </p>

                    </body>
                    </html>"
            };

        return mensaje;
    }

    private static async Task RegistrarIntentoAsync(
        SqlConnection connection,
        int idTramite,
        string canal,
        string? destinatario,
        string estadoEnvio,
        string? detalle)
    {
        const string sql = @"
            IF OBJECT_ID('dbo.TRAMITE_NOTIFICACION', 'U') IS NOT NULL
            BEGIN
                INSERT INTO TRAMITE_NOTIFICACION
                (
                    IdTramite,
                    Canal,
                    Destinatario,
                    EstadoEnvio,
                    Detalle,
                    FechaIntento
                )
                VALUES
                (
                    @IdTramite,
                    @Canal,
                    @Destinatario,
                    @EstadoEnvio,
                    @Detalle,
                    GETDATE()
                );
            END;";

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@IdTramite",
            idTramite);

        command.Parameters.AddWithValue(
            "@Canal",
            canal);

        command.Parameters.AddWithValue(
            "@Destinatario",
            (object?)destinatario
            ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@EstadoEnvio",
            estadoEnvio);

        command.Parameters.AddWithValue(
            "@Detalle",
            (object?)Recortar(
                detalle ?? string.Empty,
                1000)
            ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    private static string Recortar(
        string texto,
        int maximo)
    {
        if (texto.Length <= maximo)
        {
            return texto;
        }

        return texto[..maximo];
    }

    private sealed class DatosNotificacion
    {
        public string NumeroTramite { get; init; } = string.Empty;
        public string NombreSolicitante { get; init; } = string.Empty;
        public string Correo { get; init; } = string.Empty;
        public bool EsPropietarioRegistral { get; init; }
        public bool EsSegregacionFraccionamiento { get; init; }
    }

    private sealed class ConfiguracionSmtp
    {
        public string Host { get; init; } = string.Empty;
        public int Port { get; init; }
        public string Username { get; init; } = string.Empty;
        public string PasswordEncriptado { get; init; } = string.Empty;
        public string FromEmail { get; init; } = string.Empty;
        public string FromName { get; init; } = string.Empty;
    }

    private sealed class DocumentoTramite
    {
        public string TipoDocumento { get; init; } = string.Empty;
        public string NombreArchivo { get; init; } = string.Empty;
        public DateTime? FechaEmision { get; init; }
        public int? VigenciaDias { get; init; }
    }
}
