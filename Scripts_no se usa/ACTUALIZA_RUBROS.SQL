----------------------------------------------------------------------
-- COMPARARA ACTUALIZA LA TABLA V_TA_Rubros
-- BASE DE DATOS
----------------------------------------------------------------------
-- Autor: Alberto Antunez
-- Fecha: 30/06/2026
----------------------------------------------------------------------

USE {{CENTRAL_DB}}

DECLARE @IdRubro AS nvarchar(4)
DECLARE @Descripcion AS nvarchar(50)
DECLARE @Transmision AS int
DECLARE @COLOR AS int
DECLARE @Pesable AS bit

DECLARE DataInfo CURSOR FOR
SELECT
       [IdRubro]
      ,[Descripcion]
      ,[Transmision]
      ,[COLOR]
      ,[Pesable]
FROM [V_TA_Rubros]
EXCEPT
SELECT
       [IdRubro]
      ,[Descripcion]
      ,[Transmision]
      ,[COLOR]
      ,[Pesable]
FROM [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_TA_Rubros]

OPEN DataInfo
FETCH NEXT FROM DataInfo INTO
       @IdRubro
      ,@Descripcion
      ,@Transmision
      ,@COLOR
      ,@Pesable

WHILE @@fetch_status = 0
BEGIN
    IF EXISTS(
        SELECT IdRubro
        FROM [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_TA_Rubros]
        WHERE IdRubro = @IdRubro
    )
    BEGIN
        BEGIN TRY
            UPDATE [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_TA_Rubros]
               SET [Descripcion] = @Descripcion
                  ,[Transmision] = @Transmision
                  ,[COLOR] = @COLOR
                  ,[Pesable] = @Pesable
             WHERE IdRubro = @IdRubro
        END TRY
        BEGIN CATCH
            PRINT @@ERROR
            PRINT 'error al ACTUALIZAR RUBRO ' + @IdRubro
        END CATCH
    END
    ELSE
    BEGIN
        BEGIN TRY
            INSERT INTO [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_TA_Rubros]
            (
                 [IdRubro]
                ,[Descripcion]
                ,[Transmision]
                ,[COLOR]
                ,[Pesable]
            )
            SELECT
                 @IdRubro
                ,@Descripcion
                ,@Transmision
                ,@COLOR
                ,@Pesable
            FROM [V_TA_Rubros]
            WHERE IdRubro = @IdRubro
        END TRY
        BEGIN CATCH
            PRINT @@ERROR
            PRINT 'error al INSERTAR RUBRO ' + @IdRubro
        END CATCH
    END

    FETCH NEXT FROM DataInfo INTO
           @IdRubro
          ,@Descripcion
          ,@Transmision
          ,@COLOR
          ,@Pesable
END

CLOSE DataInfo
DEALLOCATE DataInfo
