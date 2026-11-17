USE Asada_SJM;
GO

-- =========================================================
-- MTG-03 - EVALUACIÓN ADMINISTRATIVA DE TRÁMITES
-- =========================================================


-- ---------------------------------------------------------
-- 1. Vigencia configurable de documentos
-- Permite controlar documentos con 15 o 30 días.
-- ---------------------------------------------------------

IF COL_LENGTH(
    'dbo.TRAMITE_DOCUMENTO',
    'VigenciaDias'
) IS NULL
BEGIN
    ALTER TABLE dbo.TRAMITE_DOCUMENTO
    ADD VigenciaDias INT NULL;
END;
GO


-- La certificación literal implementada en MTG-02
-- tiene vigencia máxima de 15 días.
UPDATE dbo.TRAMITE_DOCUMENTO
SET VigenciaDias = 15
WHERE TipoDocumento = 'CertificacionLiteral'
  AND VigenciaDias IS NULL;
GO


-- ---------------------------------------------------------
-- 2. Evaluación de disponibilidad hídrica
-- Una evaluación por trámite.
-- ---------------------------------------------------------

IF OBJECT_ID(
    'dbo.EVALUACION_DISPONIBILIDAD_HIDRICA',
    'U'
) IS NULL
BEGIN
    CREATE TABLE dbo.EVALUACION_DISPONIBILIDAD_HIDRICA
    (
        IdEvaluacion INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_EvaluacionDisponibilidad
            PRIMARY KEY,

        IdTramite INT NOT NULL,

        IdUsuarioEvaluador INT NOT NULL,

        Requisito1 BIT NOT NULL
            CONSTRAINT DF_Evaluacion_Requisito1 DEFAULT 0,

        Requisito2 BIT NOT NULL
            CONSTRAINT DF_Evaluacion_Requisito2 DEFAULT 0,

        Requisito3 BIT NOT NULL
            CONSTRAINT DF_Evaluacion_Requisito3 DEFAULT 0,

        Requisito4 BIT NOT NULL
            CONSTRAINT DF_Evaluacion_Requisito4 DEFAULT 0,

        Requisito5 BIT NOT NULL
            CONSTRAINT DF_Evaluacion_Requisito5 DEFAULT 0,

        Requisito6 BIT NOT NULL
            CONSTRAINT DF_Evaluacion_Requisito6 DEFAULT 0,

        Requisito7 BIT NOT NULL
            CONSTRAINT DF_Evaluacion_Requisito7 DEFAULT 0,

        Requisito8 BIT NOT NULL
            CONSTRAINT DF_Evaluacion_Requisito8 DEFAULT 0,

        Requisito9 BIT NOT NULL
            CONSTRAINT DF_Evaluacion_Requisito9 DEFAULT 0,

        EstadoEvaluacion VARCHAR(30) NOT NULL,

        MotivoRechazo VARCHAR(1000) NULL,

        RutaConstancia VARCHAR(500) NULL,

        FechaEvaluacion DATETIME NOT NULL
            CONSTRAINT DF_Evaluacion_Fecha
            DEFAULT GETDATE(),

        CONSTRAINT FK_Evaluacion_Tramite
            FOREIGN KEY (IdTramite)
            REFERENCES dbo.TRAMITE(IdTramite),

        CONSTRAINT FK_Evaluacion_Usuario
            FOREIGN KEY (IdUsuarioEvaluador)
            REFERENCES dbo.USUARIO(IdUsuario),

        CONSTRAINT UQ_Evaluacion_IdTramite
            UNIQUE (IdTramite)
    );
END;
GO


-- ---------------------------------------------------------
-- 3. Verificación
-- ---------------------------------------------------------

SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'EVALUACION_DISPONIBILIDAD_HIDRICA'
ORDER BY ORDINAL_POSITION;

SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'TRAMITE_DOCUMENTO'
ORDER BY ORDINAL_POSITION;
GO