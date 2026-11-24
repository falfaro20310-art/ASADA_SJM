USE Asada_SJM;
GO

-- =========================================================
-- MTG-02 - DOCUMENTOS DE TRÁMITE
-- =========================================================


-- ---------------------------------------------------------
-- 1. Tipo de documento
-- ---------------------------------------------------------

IF COL_LENGTH(
    'dbo.TRAMITE_DOCUMENTO',
    'TipoDocumento'
) IS NULL
BEGIN
    ALTER TABLE dbo.TRAMITE_DOCUMENTO
    ADD TipoDocumento VARCHAR(100) NULL;
END;
GO


-- ---------------------------------------------------------
-- 2. Fecha de emisión
-- Permite validar documentos con vigencia limitada.
-- ---------------------------------------------------------

IF COL_LENGTH(
    'dbo.TRAMITE_DOCUMENTO',
    'FechaEmision'
) IS NULL
BEGIN
    ALTER TABLE dbo.TRAMITE_DOCUMENTO
    ADD FechaEmision DATE NULL;
END;
GO


-- ---------------------------------------------------------
-- 3. Identificar si la solicitud corresponde a
-- segregación o fraccionamiento
-- ---------------------------------------------------------

IF COL_LENGTH(
    'dbo.SOLICITUD_DISPONIBILIDAD_HIDRICA',
    'EsSegregacionFraccionamiento'
) IS NULL
BEGIN
    ALTER TABLE dbo.SOLICITUD_DISPONIBILIDAD_HIDRICA
    ADD EsSegregacionFraccionamiento BIT NOT NULL
        CONSTRAINT DF_SolicitudDisponibilidad_Segregacion
        DEFAULT 0;
END;
GO


-- ---------------------------------------------------------
-- 4. Clasificar documentos creados previamente por MTG-01
-- ---------------------------------------------------------

UPDATE dbo.TRAMITE_DOCUMENTO
SET TipoDocumento = 'Identificacion'
WHERE TipoDocumento IS NULL
  AND NombreArchivo LIKE 'Identificación vigente%';
GO


UPDATE dbo.TRAMITE_DOCUMENTO
SET TipoDocumento = 'AutorizacionPropietario'
WHERE TipoDocumento IS NULL
  AND NombreArchivo LIKE 'Autorización autenticada%';
GO


-- ---------------------------------------------------------
-- 5. Verificación
-- ---------------------------------------------------------

SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'TRAMITE_DOCUMENTO'
ORDER BY ORDINAL_POSITION;

SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'SOLICITUD_DISPONIBILIDAD_HIDRICA'
ORDER BY ORDINAL_POSITION;
GO