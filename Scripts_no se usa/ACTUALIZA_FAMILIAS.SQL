----------------------------------------------------------------------
-- COMPARARA ACTUALIZA LA TABLA V_TA_FAMILIAS 
-- BASE DE DATOS    
----------------------------------------------------------------------
-- Autor: Alberto Antunez
-- Fecha: 27/07/2021
----------------------------------------------------------------------

USE {{CENTRAL_DB}}

-- VARIABLES DE TABLA
DECLARE @IdFamilia AS nvarchar(15)
DECLARE @Descripcion AS nvarchar(100)
DECLARE @Transmision AS int
DECLARE @MKBase AS money
DECLARE @MkReal AS money
DECLARE @IdPolitica AS nvarchar(4) 


DECLARE DataInfo CURSOR FOR
	SELECT
           [IdFamilia]
           ,[Descripcion]
           ,[Transmision]
           ,[MKBase]
           ,[MkReal]
           ,[IdPolitica]
	FROM  V_TA_FAMILIAS
   except
	SELECT
           [IdFamilia]
           ,[Descripcion]
           ,[Transmision]
           ,[MKBase]
           ,[MkReal]
           ,[IdPolitica]
           FROM [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_TA_FAMILIAS]
OPEN DataInfo
FETCH NEXT FROM DataInfo INTO 
		@IdFamilia 
		,@Descripcion
		,@Transmision
		,@MKBase  
		,@MkReal  
		,@IdPolitica
		WHILE @@fetch_status = 0
BEGIN
	IF EXISTS(SELECT IDFAMILIA FROM [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_TA_FAMILIAS] WHERE IDFAMILIA  = @IDFAMILIA ) 	
    begin
		BEGIN TRY
		UPDATE [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_TA_FAMILIAS] set  	
           [Descripcion] = @Descripcion
           ,[Transmision] = @Transmision
           ,[MKBase] = @MKBase
           ,[MkReal] =  @MkReal
           ,[IdPolitica] =  @IdPolitica
		WHERE  IDFAMILIA  = @IDFAMILIA
		END TRY
		BEGIN CATCH
			PRINT @@ERROR
			print 'error al grabar' + @IDFAMILIA  
		END CATCH
			
	 
	END		
ELSE
	BEGIN
			BEGIN TRY
			 INSERT INTO [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_TA_FAMILIAS]
				([IdFamilia]
			   ,[Descripcion]
			   ,[Transmision]
			   ,[MKBase]
			   ,[MkReal]
			   ,[IdPolitica])
			 SELECT 
				@IdFamilia 
				,@Descripcion
				,@TransmisioN
				,@MKBase  
				,@MkReal  
				,@IdPolitica
			 FROM V_TA_FAMILIAS WHERE IDFAMILIA = @IDFAMILIA	
			 END TRY
			 BEGIN CATCH
				PRINT @@ERROR
				print 'error al INSERTAR' + @IDFAMILIA  
			END CATCH		 
	END 
	  
			  
	 FETCH NEXT FROM DataInfo INTO 
		@IdFamilia 
		,@Descripcion
		,@TransmisioN
		,@MKBase  
		,@MkReal  
		,@IdPolitica
	END
 
CLOSE  DataInfo
DEALLOCATE  DataInfo
