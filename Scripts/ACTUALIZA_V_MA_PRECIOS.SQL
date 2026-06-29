----------------------------------------------------------------------
-- COMPARARA ACTUALIZA LA TABLA v_ma_precios
-- BASE DE DATOS   
----------------------------------------------------------------------
-- Autor: Alberto Antunez
-- Fecha: 21/01/2023
----------------------------------------------------------------------

USE {{CENTRAL_DB}}
   
      DECLARE @IdLista AS nvarchar(4)
      DECLARE @Nombre AS  nvarchar(50)
      DECLARE @IdArticulo AS  nvarchar(25)
      DECLARE @ConIVA AS  bit
      DECLARE @Precio1 AS  money
      DECLARE @Precio2 AS  money
      DECLARE @Precio3 AS  money
      DECLARE @Precio4 AS  money
      DECLARE @Precio5 AS  money
      DECLARE @IdMoneda AS  nvarchar(4)
      DECLARE @TipoLista AS  nvarchar(1)
      DECLARE @FCOSTO AS  nvarchar(100)
      DECLARE @FCLASE1 AS  nvarchar(100)
      DECLARE @FCLASE2 AS  nvarchar(100)
      DECLARE @FCLASE3 AS  nvarchar(100)
      DECLARE @FCLASE4 AS  nvarchar(100)
      DECLARE @FCLASE5 AS  nvarchar(100)
      DECLARE @COSTO AS  money
      DECLARE @ActualizaBase AS  bit
      DECLARE @POLITICAPRECIOS AS  nvarchar(4)
      DECLARE @CUENTAPROVEEDOR AS  nvarchar(15)
      DECLARE @IDARTICULOPROVEEDOR AS  nvarchar(25)
      DECLARE @DESCRIPCIONARTICULO AS  nvarchar(100)
      DECLARE @RUBRO AS  nvarchar(4)
      DECLARE @TIPO AS  nvarchar(4)
      DECLARE @IdUnidad AS  nvarchar(4)
      DECLARE @DesdeTrigger AS  int
      DECLARE @FhOfertaDesde AS  datetime
      DECLARE @FhOfertaHasta AS  datetime
      DECLARE @CantidadDesde AS  float
      DECLARE @GRUPO AS  nvarchar(10)
      DECLARE @Precio0 AS money
      DECLARE @PRECIO6 AS  money
      DECLARE @PRECIO7 AS  money
      DECLARE @UTILIDAD AS  float
      DECLARE @FCLASE6 AS  nvarchar(100)
      DECLARE @FCLASE7 AS  nvarchar(100)
      DECLARE @PRECIO8 AS  money
      DECLARE @FCLASE8 AS  nvarchar(100)
      DECLARE @ModificaPrecios AS nvarchar(25)
      DECLARE @Dto1 AS  float
      DECLARE @Dto2 AS  float
      DECLARE @Dto3 AS  float
      DECLARE @Dto4 AS float
      DECLARE @Dto5 AS float
      DECLARE @Dto6 AS float
      DECLARE @Dto7 AS float
      DECLARE @Dto8 AS float
      DECLARE @Dto9 AS float
      DECLARE @Rec1 AS float
      DECLARE @Rec2 AS float
      DECLARE @Rec3 AS float
      DECLARE @IdTarifaFlete AS varchar(4)
      DECLARE @PorcSeguro    AS float
      DECLARE @FhDtoDesde    AS datetime
      DECLARE @FhDtoHasta    AS datetime
      DECLARE @IDCOMPULSA    AS int
      DECLARE @IDINSERT      AS int
      DECLARE @ESTADO        AS nvarchar(10)
      DECLARE @MKTeorico     AS  money
      DECLARE @CambioCodBarra AS  bit
      DECLARE @Transmision AS  int           --- no
      DECLARE @USUARIO AS  nvarchar(50)
      DECLARE @FHALTA AS  datetime
      DECLARE @PRECIO9      AS  money
      DECLARE @CantidadOf2  AS  float
      DECLARE @PRECIO10     AS   money
      DECLARE @CantidadOf3  AS  float
 

DECLARE DataInfo CURSOR FOR
SELECT 
		   [IdLista]
           ,[Nombre]
           ,[IdArticulo]
           ,[ConIVA]
           ,[Precio1]
           ,[Precio2]
           ,[Precio3]
           ,[Precio4]
           ,[Precio5]
           ,[IdMoneda]
           ,[TipoLista]
           ,[FCOSTO]
           ,[FCLASE1]
           ,[FCLASE2]
           ,[FCLASE3]
           ,[FCLASE4]
           ,[FCLASE5]
           ,[COSTO]
           ,[ActualizaBase]
           ,[POLITICAPRECIOS]
           ,[CUENTAPROVEEDOR]
           ,[IDARTICULOPROVEEDOR]
           ,[DESCRIPCIONARTICULO]
           ,[RUBRO]
           ,[TIPO]
           ,[IdUnidad]
           ,[DesdeTrigger]
           ,[FhOfertaDesde]
           ,[FhOfertaHasta]
           ,[CantidadDesde]
           ,[GRUPO]
           ,[Precio0]
           ,[PRECIO6]
           ,[PRECIO7]
           ,[UTILIDAD]
           ,[FCLASE6]
           ,[FCLASE7]
           ,[PRECIO8]
           ,[FCLASE8]
           ,[ModificaPrecios]
           ,[Dto1]
           ,[Dto2]
           ,[Dto3]
           ,[Dto4]
           ,[Dto5]
           ,[Dto6]
           ,[Dto7]
           ,[Dto8]
           ,[Dto9]
           ,[Rec1]
           ,[Rec2]
           ,[Rec3]
           ,[IdTarifaFlete]
           ,[PorcSeguro]
           ,[FhDtoDesde]
           ,[FhDtoHasta]
           ,[IDCOMPULSA]
           ,[IDINSERT]
           ,[ESTADO]
           ,[MKTeorico]
           ,[CambioCodBarra]           
           ,[USUARIO]
           ,[FHALTA]
           ,[PRECIO9]
           ,[CantidadOf2]
           ,[PRECIO10]
           ,[CantidadOf3]
	   FROM [V_MA_PRECIOS]
EXCEPT
SELECT 
		    [IdLista]
           ,[Nombre]
           ,[IdArticulo]
           ,[ConIVA]
           ,[Precio1]
           ,[Precio2]
           ,[Precio3]
           ,[Precio4]
           ,[Precio5]
           ,[IdMoneda]
           ,[TipoLista]
           ,[FCOSTO]
           ,[FCLASE1]
           ,[FCLASE2]
           ,[FCLASE3]
           ,[FCLASE4]
           ,[FCLASE5]
           ,[COSTO]
           ,[ActualizaBase]
           ,[POLITICAPRECIOS]
           ,[CUENTAPROVEEDOR]
           ,[IDARTICULOPROVEEDOR]
           ,[DESCRIPCIONARTICULO]
           ,[RUBRO]
           ,[TIPO]
           ,[IdUnidad]
           ,[DesdeTrigger]
           ,[FhOfertaDesde]
           ,[FhOfertaHasta]
           ,[CantidadDesde]
           ,[GRUPO]
           ,[Precio0]
           ,[PRECIO6]
           ,[PRECIO7]
           ,[UTILIDAD]
           ,[FCLASE6]
           ,[FCLASE7]
           ,[PRECIO8]
           ,[FCLASE8]
           ,[ModificaPrecios]
           ,[Dto1]
           ,[Dto2]
           ,[Dto3]
           ,[Dto4]
           ,[Dto5]
           ,[Dto6]
           ,[Dto7]
           ,[Dto8]
           ,[Dto9]
           ,[Rec1]
           ,[Rec2]
           ,[Rec3]
           ,[IdTarifaFlete]
           ,[PorcSeguro]
           ,[FhDtoDesde]
           ,[FhDtoHasta]
           ,[IDCOMPULSA]
           ,[IDINSERT]
           ,[ESTADO]
           ,[MKTeorico]
           ,[CambioCodBarra]           
           ,[USUARIO]
           ,[FHALTA]
           ,[PRECIO9]
           ,[CantidadOf2]
           ,[PRECIO10]
           ,[CantidadOf3]
		   FROM [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_MA_PRECIOS]
OPEN DataInfo
FETCH NEXT FROM DataInfo INTO 		   
		    @IdLista
           ,@Nombre
           ,@IdArticulo
           ,@ConIVA
           ,@Precio1
           ,@Precio2
           ,@Precio3
           ,@Precio4
           ,@Precio5
           ,@IdMoneda
           ,@TipoLista
           ,@FCOSTO
           ,@FCLASE1
           ,@FCLASE2
           ,@FCLASE3
           ,@FCLASE4
           ,@FCLASE5
           ,@COSTO
           ,@ActualizaBase
           ,@POLITICAPRECIOS
           ,@CUENTAPROVEEDOR
           ,@IDARTICULOPROVEEDOR
           ,@DESCRIPCIONARTICULO
           ,@RUBRO
           ,@TIPO
           ,@IdUnidad
           ,@DesdeTrigger
           ,@FhOfertaDesde
           ,@FhOfertaHasta
           ,@CantidadDesde
           ,@GRUPO
           ,@Precio0
           ,@PRECIO6
           ,@PRECIO7
           ,@UTILIDAD
           ,@FCLASE6
           ,@FCLASE7
           ,@PRECIO8
           ,@FCLASE8
           ,@ModificaPrecios
           ,@Dto1
           ,@Dto2
           ,@Dto3
           ,@Dto4
           ,@Dto5
           ,@Dto6
           ,@Dto7
           ,@Dto8
           ,@Dto9
           ,@Rec1
           ,@Rec2
           ,@Rec3
           ,@IdTarifaFlete
           ,@PorcSeguro
           ,@FhDtoDesde
           ,@FhDtoHasta
           ,@IDCOMPULSA
           ,@IDINSERT
           ,@ESTADO
           ,@MKTeorico
           ,@CambioCodBarra           
           ,@USUARIO
           ,@FHALTA
           ,@PRECIO9
           ,@CantidadOf2
           ,@PRECIO10
           ,@CantidadOf3		   	   
	   WHILE @@fetch_status = 0
BEGIN
	IF EXISTS(SELECT NOMBRE FROM [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_MA_PRECIOS] WHERE IdLista = @IdLista AND IdArticulo = @IdArticulo and TipoLista = @TipoLista) 	
    BEGIN
    BEGIN TRY
		UPDATE [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_MA_PRECIOS] set  	
		IdLista		=    @IdLista
		,Nombre           =@Nombre
		,IdArticulo           =@IdArticulo
		,ConIVA           =@ConIVA
		,Precio1           =@Precio1
		,Precio2           =@Precio2
		,Precio3           =@Precio3
		,Precio4           =@Precio4
		,Precio5           =@Precio5
		,IdMoneda           =@IdMoneda
		,TipoLista           =@TipoLista
		,FCOSTO           =@FCOSTO
		,FCLASE1           =@FCLASE1
		,FCLASE2           =@FCLASE2
		,FCLASE3           =@FCLASE3
		,FCLASE4           =@FCLASE4
		,FCLASE5           =@FCLASE5
		,COSTO           =@COSTO
		,ActualizaBase           =@ActualizaBase
		,POLITICAPRECIOS           =@POLITICAPRECIOS
		,CUENTAPROVEEDOR           =@CUENTAPROVEEDOR
		,IDARTICULOPROVEEDOR           =@IDARTICULOPROVEEDOR
		,DESCRIPCIONARTICULO           =@DESCRIPCIONARTICULO
		,RUBRO           =@RUBRO
		,TIPO           =@TIPO
		,IdUnidad           =@IdUnidad
		,DesdeTrigger           =@DesdeTrigger
		,FhOfertaDesde           =@FhOfertaDesde
		,FhOfertaHasta           =@FhOfertaHasta
		,CantidadDesde           =@CantidadDesde
		,GRUPO           =@GRUPO
		,Precio0           =@Precio0
		,PRECIO6           =@PRECIO6
		,PRECIO7           =@PRECIO7
		,UTILIDAD           =@UTILIDAD
		,FCLASE6           =@FCLASE6
		,FCLASE7           =@FCLASE7
		,PRECIO8           =@PRECIO8
		,FCLASE8           =@FCLASE8
		,ModificaPrecios           =@ModificaPrecios
		,Dto1           =@Dto1
		,Dto2           =@Dto2
		,Dto3           =@Dto3
		,Dto4           =@Dto4
		,Dto5           =@Dto5
		,Dto6           =@Dto6
		,Dto7           =@Dto7
		,Dto8           =@Dto8
		,Dto9           =@Dto9
		,Rec1           =@Rec1
		,Rec2           =@Rec2
		,Rec3           =@Rec3
		,IdTarifaFlete           =@IdTarifaFlete
		,PorcSeguro           =@PorcSeguro
		,FhDtoDesde           =@FhDtoDesde
		,FhDtoHasta           =@FhDtoHasta
		,IDCOMPULSA           =@IDCOMPULSA
		,IDINSERT           =@IDINSERT
		,ESTADO           =@ESTADO
		,MKTeorico           =@MKTeorico
		,CambioCodBarra                 =@CambioCodBarra           
		,USUARIO           =@USUARIO
		,FHALTA           =@FHALTA
		,PRECIO9           =@PRECIO9
		,CantidadOf2           =@CantidadOf2
		,PRECIO10           =@PRECIO10
		,CantidadOf3           =@CantidadOf3
		WHERE  IdLista = @IdLista AND IdArticulo = @IdArticulo and TipoLista = @TipoLista 	
		END TRY
		BEGIN CATCH
			PRINT @@ERROR
			print 'Error al ACTUALIZAR PRECIOS ' + LTRIM(@IdLista)		+ LTRIM(@IdArticulo)		
		END CATCH
		print '    ACTUALIZAR PRECIOS ' + LTRIM(@IdLista)		+ LTRIM(@IdArticulo)		

		END		
ELSE
	BEGIN
	BEGIN TRY
	INSERT INTO  [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_MA_PRECIOS]
				(
					[IdLista]
				   ,[Nombre]
				   ,[IdArticulo]
				   ,[ConIVA]
				   ,[Precio1]
				   ,[Precio2]
				   ,[Precio3]
				   ,[Precio4]
				   ,[Precio5]
				   ,[IdMoneda]
				   ,[TipoLista]
				   ,[FCOSTO]
				   ,[FCLASE1]
				   ,[FCLASE2]
				   ,[FCLASE3]
				   ,[FCLASE4]
				   ,[FCLASE5]
				   ,[COSTO]
				   ,[ActualizaBase]
				   ,[POLITICAPRECIOS]
				   ,[CUENTAPROVEEDOR]
				   ,[IDARTICULOPROVEEDOR]
				   ,[DESCRIPCIONARTICULO]
				   ,[RUBRO]
				   ,[TIPO]
				   ,[IdUnidad]
				   ,[DesdeTrigger]
				   ,[FhOfertaDesde]
				   ,[FhOfertaHasta]
				   ,[CantidadDesde]
				   ,[GRUPO]
				   ,[Precio0]
				   ,[PRECIO6]
				   ,[PRECIO7]
				   ,[UTILIDAD]
				   ,[FCLASE6]
				   ,[FCLASE7]
				   ,[PRECIO8]
				   ,[FCLASE8]
				   ,[ModificaPrecios]
				   ,[Dto1]
				   ,[Dto2]
				   ,[Dto3]
				   ,[Dto4]
				   ,[Dto5]
				   ,[Dto6]
				   ,[Dto7]
				   ,[Dto8]
				   ,[Dto9]
				   ,[Rec1]
				   ,[Rec2]
				   ,[Rec3]
				   ,[IdTarifaFlete]
				   ,[PorcSeguro]
				   ,[FhDtoDesde]
				   ,[FhDtoHasta]
				   ,[IDCOMPULSA]
				   ,[IDINSERT]
				   ,[ESTADO]
				   ,[MKTeorico]
				   ,[CambioCodBarra]           
				   ,[USUARIO]
				   ,[FHALTA]
				   ,[PRECIO9]
				   ,[CantidadOf2]
				   ,[PRECIO10]
				   ,[CantidadOf3]
			   )
			 SELECT 
					@IdLista
				   ,@Nombre
				   ,@IdArticulo
				   ,@ConIVA
				   ,@Precio1
				   ,@Precio2
				   ,@Precio3
				   ,@Precio4
				   ,@Precio5
				   ,@IdMoneda
				   ,@TipoLista
				   ,@FCOSTO
				   ,@FCLASE1
				   ,@FCLASE2
				   ,@FCLASE3
				   ,@FCLASE4
				   ,@FCLASE5
				   ,@COSTO
				   ,@ActualizaBase
				   ,@POLITICAPRECIOS
				   ,@CUENTAPROVEEDOR
				   ,@IDARTICULOPROVEEDOR
				   ,@DESCRIPCIONARTICULO
				   ,@RUBRO
				   ,@TIPO
				   ,@IdUnidad
				   ,@DesdeTrigger
				   ,@FhOfertaDesde
				   ,@FhOfertaHasta
				   ,@CantidadDesde
				   ,@GRUPO
				   ,@Precio0
				   ,@PRECIO6
				   ,@PRECIO7
				   ,@UTILIDAD
				   ,@FCLASE6
				   ,@FCLASE7
				   ,@PRECIO8
				   ,@FCLASE8
				   ,@ModificaPrecios
				   ,@Dto1
				   ,@Dto2
				   ,@Dto3
				   ,@Dto4
				   ,@Dto5
				   ,@Dto6
				   ,@Dto7
				   ,@Dto8
				   ,@Dto9
				   ,@Rec1
				   ,@Rec2
				   ,@Rec3
				   ,@IdTarifaFlete
				   ,@PorcSeguro
				   ,@FhDtoDesde
				   ,@FhDtoHasta
				   ,@IDCOMPULSA
				   ,@IDINSERT
				   ,@ESTADO
				   ,@MKTeorico
				   ,@CambioCodBarra           
				   ,@USUARIO
				   ,@FHALTA
				   ,@PRECIO9
				   ,@CantidadOf2
				   ,@PRECIO10
				   ,@CantidadOf3		   	   
				  FROM V_MA_PRECIOS WHERE IdLista = @IdLista AND IdArticulo = @IdArticulo and TipoLista = @TipoLista 	
				 END TRY
				 BEGIN CATCH
					PRINT @@ERROR				  
					print 'error al INSERTAR '+ @IdLista + @IdArticulo + @TipoLista 	
				END CATCH
	END
			  
	 FETCH NEXT FROM DataInfo INTO 
					@IdLista
				   ,@Nombre
				   ,@IdArticulo
				   ,@ConIVA
				   ,@Precio1
				   ,@Precio2
				   ,@Precio3
				   ,@Precio4
				   ,@Precio5
				   ,@IdMoneda
				   ,@TipoLista
				   ,@FCOSTO
				   ,@FCLASE1
				   ,@FCLASE2
				   ,@FCLASE3
				   ,@FCLASE4
				   ,@FCLASE5
				   ,@COSTO
				   ,@ActualizaBase
				   ,@POLITICAPRECIOS
				   ,@CUENTAPROVEEDOR
				   ,@IDARTICULOPROVEEDOR
				   ,@DESCRIPCIONARTICULO
				   ,@RUBRO
				   ,@TIPO
				   ,@IdUnidad
				   ,@DesdeTrigger
				   ,@FhOfertaDesde
				   ,@FhOfertaHasta
				   ,@CantidadDesde
				   ,@GRUPO
				   ,@Precio0
				   ,@PRECIO6
				   ,@PRECIO7
				   ,@UTILIDAD
				   ,@FCLASE6
				   ,@FCLASE7
				   ,@PRECIO8
				   ,@FCLASE8
				   ,@ModificaPrecios
				   ,@Dto1
				   ,@Dto2
				   ,@Dto3
				   ,@Dto4
				   ,@Dto5
				   ,@Dto6
				   ,@Dto7
				   ,@Dto8
				   ,@Dto9
				   ,@Rec1
				   ,@Rec2
				   ,@Rec3
				   ,@IdTarifaFlete
				   ,@PorcSeguro
				   ,@FhDtoDesde
				   ,@FhDtoHasta
				   ,@IDCOMPULSA
				   ,@IDINSERT
				   ,@ESTADO
				   ,@MKTeorico
				   ,@CambioCodBarra           
				   ,@USUARIO
				   ,@FHALTA
				   ,@PRECIO9
				   ,@CantidadOf2
				   ,@PRECIO10
				   ,@CantidadOf3		   	   
			END
CLOSE  DataInfo
DEALLOCATE  DataInfo
