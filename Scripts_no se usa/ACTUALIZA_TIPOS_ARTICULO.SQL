----------------------------------------------------------------------
-- COMPARARA ACTUALIZA LA TABLA V_TA_TipoArticulo
-- BASE DE DATOS
----------------------------------------------------------------------
-- Autor: Alberto Antunez
-- Fecha: 30/06/2026
----------------------------------------------------------------------

USE {{CENTRAL_DB}}

DECLARE @IdTipo AS nvarchar(4)
DECLARE @Descripcion AS nvarchar(50)
DECLARE @Transmision AS int

DECLARE DataInfo CURSOR FOR
SELECT
       [IdTipo]
      ,[Descripcion]
      ,[Transmision]
FROM [V_TA_TipoArticulo]
EXCEPT
SELECT
       [IdTipo]
      ,[Descripcion]
      ,[Transmision]
FROM [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_TA_TipoArticulo]

OPEN DataInfo
FETCH NEXT FROM DataInfo INTO
       @IdTipo
      ,@Descripcion
      ,@Transmision

WHILE @@fetch_status = 0
BEGIN
    IF EXISTS(
        SELECT IdTipo
        FROM [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_TA_TipoArticulo]
        WHERE IdTipo = @IdTipo
    )
    BEGIN
        BEGIN TRY
            UPDATE [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_TA_TipoArticulo]
               SET [Descripcion] = @Descripcion
                  ,[Transmision] = @Transmision
             WHERE IdTipo = @IdTipo
        END TRY
        BEGIN CATCH
            PRINT @@ERROR
            PRINT 'error al ACTUALIZAR TIPO ' + @IdTipo
        END CATCH
    END
    ELSE
    BEGIN
        BEGIN TRY
            INSERT INTO [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_TA_TipoArticulo]
            (
                 [IdTipo]
                ,[Descripcion]
                ,[Transmision]
            )
            SELECT
                 @IdTipo
                ,@Descripcion
                ,@Transmision
            FROM [V_TA_TipoArticulo]
            WHERE IdTipo = @IdTipo
        END TRY
        BEGIN CATCH
            PRINT @@ERROR
            PRINT 'error al INSERTAR TIPO ' + @IdTipo
        END CATCH
    END

    FETCH NEXT FROM DataInfo INTO
           @IdTipo
          ,@Descripcion
          ,@Transmision
END

CLOSE DataInfo
DEALLOCATE DataInfo
