USE Asada_SJM;
GO

-- =========================================================
-- MTG-01 - SOLICITUD DE DISPONIBILIDAD HÍDRICA
-- =========================================================

-- Registrar tipo de trámite
IF NOT EXISTS
(
    SELECT 1
    FROM TIPO_TRAMITE
    WHERE Nombre = 'Disponibilidad Hídrica'
)
BEGIN
    INSERT INTO TIPO_TRAMITE
    (
        Nombre,
        Descripcion,
        Estado
    )
    VALUES
    (
        'Disponibilidad Hídrica',
        'Solicitud de disponibilidad de agua para conexión o construcción.',
        1
    );
END;
GO


-- Crear tabla específica para disponibilidad hídrica
IF OBJECT_ID('dbo.SOLICITUD_DISPONIBILIDAD_HIDRICA', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SOLICITUD_DISPONIBILIDAD_HIDRICA
    (
        IdSolicitudDisponibilidad INT IDENTITY(1,1) NOT NULL,

        IdTramite INT NOT NULL,

        NumeroTramite VARCHAR(30) NOT NULL,

        NombreSolicitante VARCHAR(200) NOT NULL,

        PropositoDisponibilidad VARCHAR(250) NOT NULL,

        DescripcionProyecto VARCHAR(1000) NOT NULL,

        EsPropietarioRegistral BIT NOT NULL,

        FechaRegistro DATETIME NOT NULL
            CONSTRAINT DF_SolicitudDisponibilidad_FechaRegistro
            DEFAULT GETDATE(),

        CONSTRAINT PK_SolicitudDisponibilidad
            PRIMARY KEY (IdSolicitudDisponibilidad),

        CONSTRAINT FK_SolicitudDisponibilidad_Tramite
            FOREIGN KEY (IdTramite)
            REFERENCES dbo.TRAMITE(IdTramite),

        CONSTRAINT UQ_SolicitudDisponibilidad_IdTramite
            UNIQUE (IdTramite),

        CONSTRAINT UQ_SolicitudDisponibilidad_NumeroTramite
            UNIQUE (NumeroTramite)
    );
END;
GO