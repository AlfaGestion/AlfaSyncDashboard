----------------------------------------------------------------------
-- COMPARARA ACTUALIZA LA TABLA V_TA_CategoriaArticulo
-- BASE DE DATOS
----------------------------------------------------------------------
-- Autor: Alberto Antunez
-- Fecha: 30/06/2026
----------------------------------------------------------------------

USE {{CENTRAL_DB}}

DECLARE @IdCategoria AS nvarchar(4)
DECLARE @Descripcion AS nvarchar(50)

DECLARE DataInfo CURSOR FOR
SELECT
       [IdCategoria]
      ,[Descripcion]
FROM [V_TA_CategoriaArticulo]
EXCEPT
SELECT
       [IdCategoria]
      ,[Descripcion]
FROM [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_TA_CategoriaArticulo]

OPEN DataInfo
FETCH NEXT FROM DataInfo INTO
       @IdCategoria
      ,@Descripcion

WHILE @@fetch_status = 0
BEGIN
    IF EXISTS(
        SELECT IdCategoria
        FROM [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_TA_CategoriaArticulo]
        WHERE IdCategoria = @IdCategoria
    )
    BEGIN
        BEGIN TRY
            UPDATE [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_TA_CategoriaArticulo]
               SET [Descripcion] = @Descripcion
             WHERE IdCategoria = @IdCategoria
        END TRY
        BEGIN CATCH
            PRINT @@ERROR
            PRINT 'error al ACTUALIZAR CATEGORIA ' + @IdCategoria
        END CATCH
    END
    ELSE
    BEGIN
        BEGIN TRY
            INSERT INTO [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_TA_CategoriaArticulo]
            (
                 [IdCategoria]
                ,[Descripcion]
            )
            SELECT
                 @IdCategoria
                ,@Descripcion
            FROM [V_TA_CategoriaArticulo]
            WHERE IdCategoria = @IdCategoria
        END TRY
        BEGIN CATCH
            PRINT @@ERROR
            PRINT 'error al INSERTAR CATEGORIA ' + @IdCategoria
        END CATCH
    END

    FETCH NEXT FROM DataInfo INTO
           @IdCategoria
          ,@Descripcion
END

CLOSE DataInfo
DEALLOCATE DataInfo
