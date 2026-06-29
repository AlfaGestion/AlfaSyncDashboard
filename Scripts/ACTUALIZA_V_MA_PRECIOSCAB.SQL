----------------------------------------------------------------------
-- COMPARARA ACTUALIZA LA TABLA v_ma_precioscab
-- BASE DE DATOS    
----------------------------------------------------------------------
-- Autor: Alberto Antunez
-- Fecha: 21/01/2023
----------------------------------------------------------------------

USE {{CENTRAL_DB}}

   DECLARE @Nombre        AS nvarchar(50)
   DECLARE @Grupo         AS nvarchar(10)
   DECLARE @IdLista       AS nvarchar(4)
   DECLARE @VigenciaDesde AS datetime
   DECLARE @VigenciaHasta AS datetime
   DECLARE @TipoLista     AS  nvarchar(1)
   DECLARE @Transmision   AS  int


DECLARE DataInfo CURSOR FOR
SELECT 
		[IdLista]
	   ,[Nombre]
	   ,[Grupo]
	   ,[VigenciaDesde]
	   ,[VigenciaHasta]
	   ,[TipoLista]           
	   FROM [V_MA_PRECIOSCAB]
EXCEPT
SELECT 
		[IdLista]
	   ,[Nombre]
	   ,[Grupo]
	   ,[VigenciaDesde]
	   ,[VigenciaHasta]
	   ,[TipoLista]           
  FROM  [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_MA_PRECIOSCAB]
OPEN DataInfo
FETCH NEXT FROM DataInfo INTO 
	   @IdLista      
	   ,@Nombre       
	   ,@Grupo        
	   ,@VigenciaDesde
	   ,@VigenciaHasta
	   ,@TipoLista    
	   WHILE @@fetch_status = 0
BEGIN
	IF EXISTS(SELECT NOMBRE FROM  [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_MA_PRECIOSCAB] WHERE IdLista = @IdLista AND TipoLista = @TipoLista) 	
    BEGIN
    BEGIN TRY
		UPDATE  [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_MA_PRECIOSCAB] set  	
		 Nombre         = @Nombre       
	     ,Grupo          = @Grupo        
	     ,IdLista        = @IdLista      
	     ,VigenciaDesde  = @VigenciaDesde
	     ,VigenciaHasta  = @VigenciaHasta
	     ,TipoLista      = @TipoLista    
		WHERE  IdLista = @IdLista AND TipoLista = @TipoLista		
		END TRY
		BEGIN CATCH
			PRINT @@ERROR
			print 'Error al ACTUALIZAR PRECIOSCAB ' + LTRIM(@IdLista)		
		END CATCH
		print '    ACTUALIZAR PRECIOSCAB ' + LTRIM(@IdLista)		

		END		
ELSE
	BEGIN
	BEGIN TRY
	INSERT INTO   [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_MA_PRECIOSCAB]
				(
				 Nombre        
				 ,Grupo         
				 ,IdLista       
				 ,VigenciaDesde 
				 ,VigenciaHasta 
				 ,TipoLista     
			   )
			 SELECT 
				 @Nombre        
				 ,@Grupo         
				 ,@IdLista       
				 ,@VigenciaDesde 
				 ,@VigenciaHasta 
				 ,@TipoLista     
				  FROM V_MA_PRECIOSCAB WHERE IdLista = @IdLista AND TipoLista = @TipoLista
				 END TRY
				 BEGIN CATCH
					PRINT @@ERROR				  
					print 'error al INSERTAR '+ @IdLista
				END CATCH
	END
			  
	 FETCH NEXT FROM DataInfo INTO 
				  @IdLista       
				 ,@Nombre        
				 ,@Grupo         
				 ,@VigenciaDesde 
				 ,@VigenciaHasta 
				 ,@TipoLista     				   
			END
CLOSE  DataInfo
DEALLOCATE  DataInfo
