USE Asada_SJM;
GO

/* =========================================================
   MTG-04
   Registro de notificaciones de cambios de estado
   ========================================================= */

IF OBJECT_ID('dbo.TRAMITE_NOTIFICACION', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TRAMITE_NOTIFICACION
    (
        IdNotificacion INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_TRAMITE_NOTIFICACION PRIMARY KEY,

        IdTramite INT NOT NULL,

        Canal VARCHAR(20) NOT NULL,

        Destinatario VARCHAR(256) NULL,

        EstadoEnvio VARCHAR(20) NOT NULL,

        Detalle VARCHAR(1000) NULL,

        FechaIntento DATETIME NOT NULL
            CONSTRAINT DF_TRAMITE_NOTIFICACION_FechaIntento
            DEFAULT GETDATE(),

        CONSTRAINT FK_TRAMITE_NOTIFICACION_TRAMITE
            FOREIGN KEY (IdTramite)
            REFERENCES dbo.TRAMITE(IdTramite)
    );
END;
GO

/* =========================================================
   Garantizar vigencia de 15 días para nuevas
   certificaciones literales.
   MTG-02 validaba la fecha de ingreso, pero los documentos
   nuevos también deben conservar la vigencia para MTG-03/04.
   ========================================================= */

CREATE OR ALTER TRIGGER dbo.TRG_TRAMITE_DOCUMENTO_VigenciaCertificacion
ON dbo.TRAMITE_DOCUMENTO
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE td
    SET td.VigenciaDias = 15
    FROM dbo.TRAMITE_DOCUMENTO td
    INNER JOIN inserted i
        ON i.IdDocumento = td.IdDocumento
    WHERE
        td.TipoDocumento = 'CertificacionLiteral'
        AND td.VigenciaDias IS NULL;
END;
GO

/* Corregir cualquier certificación existente que aún no
   tenga vigencia configurada. */
UPDATE dbo.TRAMITE_DOCUMENTO
SET VigenciaDias = 15
WHERE
    TipoDocumento = 'CertificacionLiteral'
    AND VigenciaDias IS NULL;
GO

SELECT
    IdNotificacion,
    IdTramite,
    Canal,
    Destinatario,
    EstadoEnvio,
    Detalle,
    FechaIntento
FROM dbo.TRAMITE_NOTIFICACION
ORDER BY IdNotificacion DESC;
GO

/* =========================================================
   Compatibilidad con dashboard de abonados
   HomeController utiliza FechaRegistro para estadísticas.
   ========================================================= */

IF COL_LENGTH('dbo.ABONADO', 'FechaRegistro') IS NULL
BEGIN
    ALTER TABLE dbo.ABONADO
    ADD FechaRegistro DATETIME NULL;

    ALTER TABLE dbo.ABONADO
    ADD CONSTRAINT DF_ABONADO_FechaRegistro
        DEFAULT GETDATE() FOR FechaRegistro;
END;
GO
