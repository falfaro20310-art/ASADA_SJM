USE Asada_SJM;
GO

-- =========================================================
-- MTI-04 - SUSCRIPCIÓN A NOTIFICACIONES
-- =========================================================
-- Permite que un usuario se suscriba a una o varias
-- categorías de documentos de Transparencia Institucional.
-- =========================================================

IF OBJECT_ID('dbo.SUSCRIPCION_DOCUMENTO', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SUSCRIPCION_DOCUMENTO
    (
        IdSuscripcion INT IDENTITY(1,1) NOT NULL,
        IdUsuario INT NOT NULL,
        IdCategoria INT NOT NULL,
        FechaSuscripcion DATETIME NOT NULL
            CONSTRAINT DF_SuscripcionDocumento_FechaSuscripcion
            DEFAULT GETDATE(),
        Estado BIT NOT NULL
            CONSTRAINT DF_SuscripcionDocumento_Estado
            DEFAULT 1,

        CONSTRAINT PK_SuscripcionDocumento
            PRIMARY KEY (IdSuscripcion),

        CONSTRAINT FK_SuscripcionDocumento_Usuario
            FOREIGN KEY (IdUsuario)
            REFERENCES dbo.USUARIO(IdUsuario),

        CONSTRAINT FK_SuscripcionDocumento_Categoria
            FOREIGN KEY (IdCategoria)
            REFERENCES dbo.CATEGORIA_DOCUMENTO(IdCategoria),

        CONSTRAINT UQ_SuscripcionDocumento_UsuarioCategoria
            UNIQUE (IdUsuario, IdCategoria)
    );
END;
GO