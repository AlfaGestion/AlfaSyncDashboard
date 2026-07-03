/***********************************************************************************************
Autor: Alberto / IA
Fecha: 02/07/2026
Este scrtipt crea la tabla necesaria y el trigger
***********************************************************************************************/
USE xxxxxxxxx
GO

/****** Objeto: Table [dbo].[SYNC_PRECIOS_SERVER] Fecha de script: 02/07/2026 18:11:59 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[SYNC_PRECIOS_SERVER](
	[IdSync] [int] IDENTITY(1,1) NOT NULL,
	[FechaHora] [datetime] NOT NULL,
	[IdArticulo] [nvarchar](25) NOT NULL,
	[IdLista] [nvarchar](4) NULL,
	[Usuario] [nvarchar](50) NULL,
	[Estado] [nvarchar](20) NOT NULL,
	[FechaProcesado] [datetime] NULL,
	[Error] [nvarchar](4000) NULL,
	[FechaUltCambio] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdSync] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[SYNC_PRECIOS_SERVER] ADD  DEFAULT ('PENDIENTE') FOR [Estado]
GO



/****** Objeto: Trigger [dbo].[TR_V_MV_PreciosHis_SyncServer] Fecha de script: 02/07/2026 18:13:05 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

 
CREATE TRIGGER [dbo].[TR_V_MV_PreciosHis_SyncServer]
ON [dbo].[V_MV_PreciosHis]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Cambios AS
    (
        SELECT
            MAX(FechaHora) AS FechaHora,
            IdArticulo,
            IdLista,
            MAX(Usuario) AS Usuario
        FROM inserted
        WHERE Usuario = 'Marcos'
        GROUP BY
            IdArticulo,
            IdLista
    )
    MERGE dbo.SYNC_PRECIOS_SERVER AS D
    USING Cambios AS O
        ON  D.IdArticulo = O.IdArticulo
        AND (
                D.IdLista = O.IdLista
             OR (D.IdLista IS NULL AND O.IdLista IS NULL)
            )
        AND D.Estado = 'PENDIENTE'

    WHEN MATCHED THEN
        UPDATE SET
            D.FechaHora      = O.FechaHora,
            D.FechaUltCambio = O.FechaHora,
            D.Usuario        = O.Usuario,
            D.Error          = NULL

    WHEN NOT MATCHED THEN
        INSERT
        (
            FechaHora,
            FechaUltCambio,
            IdArticulo,
            IdLista,
            Usuario,
            Estado
        )
        VALUES
        (
            O.FechaHora,
            O.FechaHora,
            O.IdArticulo,
            O.IdLista,
            O.Usuario,
            'PENDIENTE'
        );
END;
GO

ALTER TABLE [dbo].[V_MV_PreciosHis] ENABLE TRIGGER [TR_V_MV_PreciosHis_SyncServer]
GO

