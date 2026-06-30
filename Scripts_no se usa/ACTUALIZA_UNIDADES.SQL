----------------------------------------------------------------------
-- COMPARARA ACTUALIZA LA TABLA V_TA_Unidad
-- BASE DE DATOS
----------------------------------------------------------------------
-- Autor: Alberto Antunez
-- Fecha: 30/06/2026
----------------------------------------------------------------------

USE {{CENTRAL_DB}}

DECLARE @IdUnidad AS nvarchar(4)
DECLARE @Descripcion AS nvarchar(50)
DECLARE @Transmision AS int

DECLARE DataInfo CURSOR FOR
SELECT
       [IdUnidad]
      ,[Descripcion]
      ,[Transmision]
FROM [V_TA_Unidad]
EXCEPT
SELECT
       [IdUnidad]
      ,[Descripcion]
      ,[Transmision]
FROM [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_TA_Unidad]

OPEN DataInfo
FETCH NEXT FROM DataInfo INTO
       @IdUnidad
      ,@Descripcion
      ,@Transmision

WHILE @@fetch_status = 0
BEGIN
    IF EXISTS(
        SELECT IdUnidad
        FROM [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_TA_Unidad]
        WHERE IdUnidad = @IdUnidad
    )
    BEGIN
        BEGIN TRY
            UPDATE [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_TA_Unidad]
               SET [Descripcion] = @Descripcion
                  ,[Transmision] = @Transmision
             WHERE IdUnidad = @IdUnidad
        END TRY
        BEGIN CATCH
            PRINT @@ERROR
            PRINT 'error al ACTUALIZAR UNIDAD ' + @IdUnidad
        END CATCH
    END
    ELSE
    BEGIN
        BEGIN TRY
            INSERT INTO [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_TA_Unidad]
            (
                 [IdUnidad]
                ,[Descripcion]
                ,[Transmision]
            )
            SELECT
                 @IdUnidad
                ,@Descripcion
                ,@Transmision
            FROM [V_TA_Unidad]
            WHERE IdUnidad = @IdUnidad
        END TRY
        BEGIN CATCH
            PRINT @@ERROR
            PRINT 'error al INSERTAR UNIDAD ' + @IdUnidad
        END CATCH
    END

    FETCH NEXT FROM DataInfo INTO
           @IdUnidad
          ,@Descripcion
          ,@Transmision
END

CLOSE DataInfo
DEALLOCATE DataInfo
