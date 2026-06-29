----------------------------------------------------------------------
-- COMPARARA ACTUALIZA LA TABLA v_ma_articulos 
-- BASE DE DATOS   
----------------------------------------------------------------------
-- Autor: Alberto Antunez
-- Fecha: 23/04/2020
----------------------------------------------------------------------

USE {{CENTRAL_DB}}

    DECLARE	@IDARTICULO  AS  nvarchar   (25)  
	DECLARE	@CODIGOBARRA AS  nvarchar   (25) 
	DECLARE	@DESCRIPCION AS  nvarchar   (50)  
	DECLARE	@CUENTAPROVEEDOR AS  nvarchar   (15)    --- NO ACTUALIZO 
	DECLARE	@IDUNIDAD AS  nvarchar   (4) 
	DECLARE	@IDRUBRO AS  nvarchar   (4) 
	DECLARE	@IDTIPO AS  nvarchar   (4) 
	DECLARE	@USASERIE AS  bit   
	DECLARE	@USALOTE AS  bit   
	DECLARE	@EXENTO AS  bit   
	DECLARE	@NOTAS AS  nvarchar   (250) 
	DECLARE	@PUNTOPEDIDO AS  int  
	DECLARE	@COSTO AS  money    
	DECLARE	@IMPUESTOS AS  money    
	DECLARE	@PRECIO1 AS  money    
	DECLARE	@PRECIO2 AS  money    
	DECLARE	@PRECIO3 AS  money    
	DECLARE	@PRECIO4 AS  money    
	DECLARE	@PRECIO5 AS  money    
	DECLARE	@SUSPENDIDO AS  bit   
	DECLARE	@PUNTOMAXIMO AS  int  
	DECLARE	@PoliticaPrecios AS  nvarchar   (4) 
	DECLARE	@TasaIVA AS  float  
	DECLARE	@Moneda AS  nvarchar   (4) 
	DECLARE	@RutaImagen AS  nvarchar   (100) 
	DECLARE	@IdPercepcion AS  nvarchar   (4) 
	DECLARE	@Usuario AS  nvarchar   (50) 
	DECLARE	@Observaciones AS  nvarchar   (250) 
	DECLARE	@ActualizaCosto AS  bit  
	DECLARE	@CuentaContableC AS  nvarchar   (15)    -- NO ACTUALIZO
	DECLARE	@CuentaContable AS  nvarchar   (15)     -- NO ACTUALIZO
	DECLARE	@IdFamilia AS  nvarchar   (15) 
	DECLARE	@UD_CPRA AS  nvarchar   (4) 
	DECLARE	@UD_STOCK AS  nvarchar   (4) 
	DECLARE	@UD_TTE AS  nvarchar   (4) 
	DECLARE	@Presentacion AS  nvarchar   (50) 
	DECLARE	@DescripAbrev AS  nvarchar   (50) 
	DECLARE	@CostoInsumos AS  money    
	DECLARE	@DesdeTrigger AS  int  
	DECLARE	@CodigoBarraDun AS  nvarchar   (25) 
	DECLARE	@Perecedero AS  bit  
	DECLARE	@Pesable AS  bit  
	DECLARE	@PRECIO6 AS  money    
	DECLARE	@PRECIO7 AS  money    
	DECLARE	@UTILIDAD AS  float  
	DECLARE	@CODIGOBARRA1 AS  nvarchar   (25) 
	DECLARE	@CODIGOBARRA2 AS  nvarchar   (25) 
	DECLARE	@CODIGOBARRA3 AS  nvarchar   (25) 
	DECLARE	@CODIGOBARRA4 AS  nvarchar   (25) 
	DECLARE	@UMCB1 AS  nvarchar   (4) 
	DECLARE	@UMCB2 AS  nvarchar   (4) 
	DECLARE	@UMCB3 AS  nvarchar   (4) 
	DECLARE	@UMCB4 AS  nvarchar   (4) 
	DECLARE	@PRECIO8 AS  money    
	DECLARE	@KILOS AS  float  
	DECLARE	@M3 AS  float  
	DECLARE	@SuspendidoV AS  BIT    
	DECLARE	@SuspendidoC AS  BIT    
	DECLARE	@Dto1 AS  float  
	DECLARE	@Dto2 AS  float  
	DECLARE	@Dto3 AS  float  
	DECLARE	@Dto4 AS  float  
	DECLARE	@Dto5 AS  float  
	DECLARE	@Rec1 AS  float  
	DECLARE	@Rec2 AS  float  
	DECLARE	@Rec3 AS  float  
	DECLARE	@IdTarifaFlete AS  nvarchar   (4) 
	DECLARE	@PorcSeguro AS  float  
	DECLARE	@Dto6 AS  float  
	DECLARE	@Dto7 AS  float  
	DECLARE	@Dto8 AS  float  
	DECLARE	@Dto9 AS  float  
	DECLARE	@FhUltimoCosto AS datetime  
	DECLARE	@FhDtoDesde AS datetime  
	DECLARE	@FhDtoHasta AS datetime  
	DECLARE	@UsaTalle AS  bit  
	DECLARE	@CodigoArtProveedor AS  nvarchar   (25) 
	DECLARE	@TalleDefault AS  nvarchar   (50) 
	DECLARE	@ColorDefault AS  nvarchar   (50) 
	DECLARE	@SexoDefault AS  nvarchar   (1) 
	DECLARE	@InsumosPorPorcentaje AS  bit  
	DECLARE	@EnOferta AS  bit  
	DECLARE	@Espesor AS  float  
	DECLARE	@Ancho AS  float  
	DECLARE	@Largo AS  float  
	DECLARE	@PideMedidas AS  bit  
	DECLARE	@EnComodato AS  bit  
	DECLARE	@Transmision AS  int  
	DECLARE	@PideEquivalencia AS  bit  
	DECLARE	@ITC AS  money    
	DECLARE	@FHALTA AS datetime  
	DECLARE	@InsertaObserv AS  bit  
	DECLARE	@NO_CONTROLA_STOCK AS  bit  
	DECLARE	@URL1 AS  nvarchar   (150) 
	DECLARE	@Ubicacion_Habitual AS  nvarchar   (50) 
	DECLARE	@Procedencia AS  nvarchar   (150) 
	DECLARE	@PideDescripcionAdicional AS  BIT    

DECLARE DataInfo CURSOR FOR
SELECT 
      [IDARTICULO]
      ,[CODIGOBARRA]
      ,[DESCRIPCION]
      ,[IDUNIDAD]
      ,[IDRUBRO]
      ,[IDTIPO]
      ,[USASERIE]
      ,[USALOTE]
      ,[EXENTO]
      ,[NOTAS]
      --,[PUNTOPEDIDO]   este dato es propio de cada sucursal
      ,[COSTO]
      ,[IMPUESTOS]
      ,[PRECIO1]
      ,[PRECIO2]
      ,[PRECIO3]
      ,[PRECIO4]
      ,[PRECIO5]
      --,[SUSPENDIDO]
      --,[PUNTOMAXIMO]    este dato es propio de cada sucursal
      ,[PoliticaPrecios]
      ,[TasaIVA]
      ,[Moneda]
      ,[RutaImagen]
      ,[IdPercepcion]
      ,[Usuario]
      ,[Observaciones]
      ,[ActualizaCosto]
      ,[IdFamilia]
      ,[UD_CPRA]
      ,[UD_STOCK]
      ,[UD_TTE]
      ,[Presentacion]
      ,[DescripAbrev]
      ,[CostoInsumos]
      ,[DesdeTrigger]
      ,[CodigoBarraDun]
      ,[Perecedero]
      ,[Pesable]
      ,[PRECIO6]
      ,[PRECIO7]
      ,[UTILIDAD]
      ,[CODIGOBARRA1]
      ,[CODIGOBARRA2]
      ,[CODIGOBARRA3]
      ,[CODIGOBARRA4]
      ,[UMCB1]
      ,[UMCB2]
      ,[UMCB3]
      ,[UMCB4]
      ,[PRECIO8]
      ,[KILOS]
      ,[M3]
      --,[SuspendidoV]
      --,[SuspendidoC]
      ,[Dto1]
      ,[Dto2]
      ,[Dto3]
      ,[Dto4]
      ,[Dto5]
      ,[Rec1]
      ,[Rec2]
      ,[Rec3]
      ,[IdTarifaFlete]
      ,[PorcSeguro]
      ,[Dto6]
      ,[Dto7]
      ,[Dto8]
      ,[Dto9]
      ,[FhUltimoCosto]
      ,[FhDtoDesde]
      ,[FhDtoHasta]
      ,[UsaTalle]
      ,[CodigoArtProveedor]
      ,[TalleDefault]
      ,[ColorDefault]
      ,[SexoDefault]
      ,[InsumosPorPorcentaje]
      ,[EnOferta]
      ,[Espesor]
      ,[Ancho]
      ,[Largo]
      ,[PideMedidas]
      ,[EnComodato]
      ,[Transmision]
      ,[PideEquivalencia]
      ,[ITC]
      ,[FHALTA]
      ,[InsertaObserv]
      ,[NO_CONTROLA_STOCK]
      ,[URL1]
      ,[Ubicacion_Habitual]
      ,[Procedencia]
      ,[PideDescripcionAdicional]
  FROM [V_MA_ARTICULOS]
EXCEPT
SELECT 
      [IDARTICULO]
      ,[CODIGOBARRA]
      ,[DESCRIPCION]
      ,[IDUNIDAD]
      ,[IDRUBRO]
      ,[IDTIPO]
      ,[USASERIE]
      ,[USALOTE]
      ,[EXENTO]
      ,[NOTAS]
      -- ,[PUNTOPEDIDO]este dato es propio de cada sucursal
      ,[COSTO]
      ,[IMPUESTOS]
      ,[PRECIO1]
      ,[PRECIO2]
      ,[PRECIO3]
      ,[PRECIO4]
      ,[PRECIO5]
      --,[SUSPENDIDO]  este dato es propio para cada sucursal  
      --,[PUNTOMAXIMO] este dato es propio de cada sucursal
      ,[PoliticaPrecios]
      ,[TasaIVA]
      ,[Moneda]
      ,[RutaImagen]
      ,[IdPercepcion]
      ,[Usuario]
      ,[Observaciones]
      ,[ActualizaCosto]
      ,[IdFamilia]
      ,[UD_CPRA]
      ,[UD_STOCK]
      ,[UD_TTE]
      ,[Presentacion]
      ,[DescripAbrev]
      ,[CostoInsumos]
      ,[DesdeTrigger]
      ,[CodigoBarraDun]
      ,[Perecedero]
      ,[Pesable]
      ,[PRECIO6]
      ,[PRECIO7]
      ,[UTILIDAD]
      ,[CODIGOBARRA1]
      ,[CODIGOBARRA2]
      ,[CODIGOBARRA3]
      ,[CODIGOBARRA4]
      ,[UMCB1]
      ,[UMCB2]
      ,[UMCB3]
      ,[UMCB4]
      ,[PRECIO8]
      ,[KILOS]
      ,[M3]
      --,[SuspendidoV]
      --,[SuspendidoC]
      ,[Dto1]
      ,[Dto2]
      ,[Dto3]
      ,[Dto4]
      ,[Dto5]
      ,[Rec1]
      ,[Rec2]
      ,[Rec3]
      ,[IdTarifaFlete]
      ,[PorcSeguro]
      ,[Dto6]
      ,[Dto7]
      ,[Dto8]
      ,[Dto9]
      ,[FhUltimoCosto]
      ,[FhDtoDesde]
      ,[FhDtoHasta]
      ,[UsaTalle]
      ,[CodigoArtProveedor]
      ,[TalleDefault]
      ,[ColorDefault]
      ,[SexoDefault]
      ,[InsumosPorPorcentaje]
      ,[EnOferta]
      ,[Espesor]
      ,[Ancho]
      ,[Largo]
      ,[PideMedidas]
      ,[EnComodato]
      ,[Transmision]
      ,[PideEquivalencia]
      ,[ITC]
      ,[FHALTA]
      ,[InsertaObserv]
      ,[NO_CONTROLA_STOCK]
      ,[URL1]
      ,[Ubicacion_Habitual]
      ,[Procedencia]
      ,[PideDescripcionAdicional]
  FROM [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_MA_ARTICULOS]
OPEN DataInfo
FETCH NEXT FROM DataInfo INTO 
	   @IDARTICULO 
      ,@CODIGOBARRA 
      ,@DESCRIPCION 
      ,@IDUNIDAD 
      ,@IDRUBRO 
      ,@IDTIPO 
      ,@USASERIE 
      ,@USALOTE 
      ,@EXENTO 
      ,@NOTAS 
      --,@PUNTOPEDIDO este dato es propio de cada sucursal
      ,@COSTO 
      ,@IMPUESTOS 
      ,@PRECIO1 
      ,@PRECIO2 
      ,@PRECIO3 
      ,@PRECIO4 
      ,@PRECIO5 
      --,@SUSPENDIDO   este dato es propio de cada sucursal
      --,@PUNTOMAXIMO este dato es propio de cada sucursal
      ,@PoliticaPrecios 
      ,@TasaIVA 
      ,@Moneda 
      ,@RutaImagen 
      ,@IdPercepcion 
      ,@Usuario 
      ,@Observaciones 
      ,@ActualizaCosto 
      ,@IdFamilia 
      ,@UD_CPRA 
      ,@UD_STOCK 
      ,@UD_TTE 
      ,@Presentacion 
      ,@DescripAbrev 
      ,@CostoInsumos 
      ,@DesdeTrigger 
      ,@CodigoBarraDun 
      ,@Perecedero 
      ,@Pesable 
      ,@PRECIO6 
      ,@PRECIO7 
      ,@UTILIDAD 
      ,@CODIGOBARRA1 
      ,@CODIGOBARRA2 
      ,@CODIGOBARRA3 
      ,@CODIGOBARRA4 
      ,@UMCB1 
      ,@UMCB2 
      ,@UMCB3 
      ,@UMCB4 
      ,@PRECIO8 
      ,@KILOS 
      ,@M3 
      --,@SuspendidoV 
      --,@SuspendidoC 
      ,@Dto1 
      ,@Dto2 
      ,@Dto3 
      ,@Dto4 
      ,@Dto5 
      ,@Rec1 
      ,@Rec2 
      ,@Rec3 
      ,@IdTarifaFlete 
      ,@PorcSeguro 
      ,@Dto6 
      ,@Dto7 
      ,@Dto8 
      ,@Dto9 
      ,@FhUltimoCosto 
      ,@FhDtoDesde 
      ,@FhDtoHasta 
      ,@UsaTalle 
      ,@CodigoArtProveedor 
      ,@TalleDefault 
      ,@ColorDefault 
      ,@SexoDefault 
      ,@InsumosPorPorcentaje 
      ,@EnOferta 
      ,@Espesor 
      ,@Ancho 
      ,@Largo 
      ,@PideMedidas 
      ,@EnComodato 
      ,@Transmision 
      ,@PideEquivalencia 
      ,@ITC 
      ,@FHALTA 
      ,@InsertaObserv 
      ,@NO_CONTROLA_STOCK 
      ,@URL1 
      ,@Ubicacion_Habitual 
      ,@Procedencia 
      ,@PideDescripcionAdicional  
	   WHILE @@fetch_status = 0
BEGIN
	IF EXISTS(SELECT IDARTICULO FROM [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_MA_ARTICULOS] WHERE IDARTICULO = @IDARTICULO) 	
    BEGIN
    BEGIN TRY
		UPDATE [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_MA_ARTICULOS] set  	
		 IDARTICULO       = @IDARTICULO 
		, CODIGOBARRA      = @CODIGOBARRA 
		, DESCRIPCION      = @DESCRIPCION 		
		, IDUNIDAD      = @IDUNIDAD 
		, IDRUBRO      = @IDRUBRO 
		, IDTIPO      = @IDTIPO 
		, USASERIE      = @USASERIE 
		, USALOTE      = @USALOTE 
		, EXENTO      = @EXENTO 
		, NOTAS      = @NOTAS 
		--, PUNTOPEDIDO      = @PUNTOPEDIDO este dato es propio de cada sucursal
		, COSTO      = @COSTO 
		, IMPUESTOS      = @IMPUESTOS 
		, PRECIO1      = @PRECIO1 
		, PRECIO2      = @PRECIO2 
		, PRECIO3      = @PRECIO3 
		, PRECIO4      = @PRECIO4 
		, PRECIO5      = @PRECIO5 
		--, SUSPENDIDO      = @SUSPENDIDO 
		--, PUNTOMAXIMO      = @PUNTOMAXIMO este dato es propio de cada sucursal
		, PoliticaPrecios      = @PoliticaPrecios 
		, TasaIVA      = @TasaIVA 
		, Moneda      = @Moneda 
		, RutaImagen      = @RutaImagen 
		, IdPercepcion      = @IdPercepcion 
		, Usuario      = @Usuario 
		, Observaciones      = @Observaciones 
		, ActualizaCosto      = @ActualizaCosto 
		, IdFamilia      = @IdFamilia 
		, UD_CPRA      = @UD_CPRA 
		, UD_STOCK      = @UD_STOCK 
		, UD_TTE      = @UD_TTE 
		, Presentacion      = @Presentacion 
		, DescripAbrev      = @DescripAbrev 
		 ,CostoInsumos      = @CostoInsumos 
		, DesdeTrigger      = @DesdeTrigger 
		, CodigoBarraDun      = @CodigoBarraDun 
		, Perecedero      = @Perecedero 
		, Pesable      = @Pesable 
		, PRECIO6      = @PRECIO6 
		, PRECIO7      = @PRECIO7 
		, UTILIDAD      = @UTILIDAD 
		, CODIGOBARRA1      = @CODIGOBARRA1 
		, CODIGOBARRA2      = @CODIGOBARRA2 
		, CODIGOBARRA3      = @CODIGOBARRA3 
		, CODIGOBARRA4      = @CODIGOBARRA4 
		, UMCB1      = @UMCB1 
		, UMCB2      = @UMCB2 
		, UMCB3      = @UMCB3 
		, UMCB4      = @UMCB4 
		, PRECIO8      = @PRECIO8 
		, KILOS      = @KILOS 
		, M3      = @M3 
		--, SuspendidoV      = @SuspendidoV 
		--, SuspendidoC      = @SuspendidoC 
		, Dto1      = @Dto1 
		, Dto2      = @Dto2 
		, Dto3      = @Dto3 
		, Dto4      = @Dto4 
		, Dto5      = @Dto5 
		, Rec1      = @Rec1 
		, Rec2      = @Rec2 
		, Rec3      = @Rec3 
		, IdTarifaFlete      = @IdTarifaFlete 
		, PorcSeguro      = @PorcSeguro 
		, Dto6      = @Dto6 
		, Dto7      = @Dto7 
		, Dto8      = @Dto8 
		, Dto9      = @Dto9 
		, FhUltimoCosto      = @FhUltimoCosto 
		, FhDtoDesde      = @FhDtoDesde 
		, FhDtoHasta      = @FhDtoHasta 
		, UsaTalle      = @UsaTalle 
		, CodigoArtProveedor      = @CodigoArtProveedor 
		, TalleDefault      = @TalleDefault 
		, ColorDefault      = @ColorDefault 
		, SexoDefault      = @SexoDefault 
		, InsumosPorPorcentaje      = @InsumosPorPorcentaje 
		, EnOferta      = @EnOferta 
		, Espesor      = @Espesor 
		, Ancho      = @Ancho 
		, Largo      = @Largo 
		, PideMedidas      = @PideMedidas 
		, EnComodato      = @EnComodato 
		, Transmision      = @Transmision 
		, PideEquivalencia      = @PideEquivalencia 
		, ITC      = @ITC 
		, FHALTA      = @FHALTA 
		, InsertaObserv      = @InsertaObserv 
		, NO_CONTROLA_STOCK      = @NO_CONTROLA_STOCK 
		, URL1      = @URL1 
		, Ubicacion_Habitual      = @Ubicacion_Habitual 
		, Procedencia      = @Procedencia 
		, PideDescripcionAdicional      = @PideDescripcionAdicional 
		WHERE  IDARTICULO   = @IDARTICULO		
		END TRY
		BEGIN CATCH
			PRINT @@ERROR
			print 'Error al ACTUALIZAR ARTICULOS ' + LTRIM(@IDARTICULO)		
		END CATCH
		print '    ACTUALIZAR ARTICULOS ' + LTRIM(@IDARTICULO)		

		END		
ELSE
	BEGIN
	BEGIN TRY
	INSERT INTO  [{{LOCAL_SERVER}}].[{{LOCAL_DB}}].[dbo].[V_MA_ARTICULOS]
				(
				   [IDARTICULO]
				  ,[CODIGOBARRA]
				  ,[DESCRIPCION]				  
				  ,[IDUNIDAD]
				  ,[IDRUBRO]
				  ,[IDTIPO]
				  ,[USASERIE]
				  ,[USALOTE]
				  ,[EXENTO]
				  ,[NOTAS]
				  --,[PUNTOPEDIDO]este dato es propio de cada sucursal
				  ,[COSTO]
				  ,[IMPUESTOS]
				  ,[PRECIO1]
				  ,[PRECIO2]
				  ,[PRECIO3]
				  ,[PRECIO4]
				  ,[PRECIO5]
				  --,[SUSPENDIDO]
				  --,[PUNTOMAXIMO]este dato es propio de cada sucursal
				  ,[PoliticaPrecios]
				  ,[TasaIVA]
				  ,[Moneda]
				  ,[RutaImagen]
				  ,[IdPercepcion]
				  ,[Usuario]
				  ,[Observaciones]
				  ,[ActualizaCosto]
				  ,[IdFamilia]
				  ,[UD_CPRA]
				  ,[UD_STOCK]
				  ,[UD_TTE]
				  ,[Presentacion]
				  ,[DescripAbrev]
				  ,[CostoInsumos]
				  ,[DesdeTrigger]
				  ,[CodigoBarraDun]
				  ,[Perecedero]
				  ,[Pesable]
				  ,[PRECIO6]
				  ,[PRECIO7]
				  ,[UTILIDAD]
				  ,[CODIGOBARRA1]
				  ,[CODIGOBARRA2]
				  ,[CODIGOBARRA3]
				  ,[CODIGOBARRA4]
				  ,[UMCB1]
				  ,[UMCB2]
				  ,[UMCB3]
				  ,[UMCB4]
				  ,[PRECIO8]
				  ,[KILOS]
				  ,[M3]
				  --,[SuspendidoV]
				  --,[SuspendidoC]
				  ,[Dto1]
				  ,[Dto2]
				  ,[Dto3]
				  ,[Dto4]
				  ,[Dto5]
				  ,[Rec1]
				  ,[Rec2]
				  ,[Rec3]
				  ,[IdTarifaFlete]
				  ,[PorcSeguro]
				  ,[Dto6]
				  ,[Dto7]
				  ,[Dto8]
				  ,[Dto9]
				  ,[FhUltimoCosto]
				  ,[FhDtoDesde]
				  ,[FhDtoHasta]
				  ,[UsaTalle]
				  ,[CodigoArtProveedor]
				  ,[TalleDefault]
				  ,[ColorDefault]
				  ,[SexoDefault]
				  ,[InsumosPorPorcentaje]
				  ,[EnOferta]
				  ,[Espesor]
				  ,[Ancho]
				  ,[Largo]
				  ,[PideMedidas]
				  ,[EnComodato]
				  ,[Transmision]
				  ,[PideEquivalencia]
				  ,[ITC]
				  ,[FHALTA]
				  ,[InsertaObserv]
				  ,[NO_CONTROLA_STOCK]
				  ,[URL1]
				  ,[Ubicacion_Habitual]
				  ,[Procedencia]
				  ,[PideDescripcionAdicional]
			   )
			 SELECT 
				    @IDARTICULO 
				  , @CODIGOBARRA 
				  , @DESCRIPCION  
				  , @IDUNIDAD 
				  , @IDRUBRO 
				  , @IDTIPO 
				  , @USASERIE 
				  , @USALOTE 
				  , @EXENTO 
				  , @NOTAS 
				 -- , @PUNTOPEDIDO 
				  , @COSTO 
				  , @IMPUESTOS 
				  , @PRECIO1 
				  , @PRECIO2 
				  , @PRECIO3 
				  , @PRECIO4 
				  , @PRECIO5 
				  --, @SUSPENDIDO 
				  --, @PUNTOMAXIMO 
				  , @PoliticaPrecios 
				  , @TasaIVA 
				  , @Moneda 
				  , @RutaImagen 
				  , @IdPercepcion 
				  , @Usuario 
				  , @Observaciones 
				  , @ActualizaCosto 
				  , @IdFamilia 
				  , @UD_CPRA 
				  , @UD_STOCK 
				  , @UD_TTE 
				  , @Presentacion 
				  , @DescripAbrev 
				  , @CostoInsumos 
				  , @DesdeTrigger 
				  , @CodigoBarraDun 
				  , @Perecedero 
				  , @Pesable 
				  , @PRECIO6 
				  , @PRECIO7 
				  , @UTILIDAD 
				  , @CODIGOBARRA1 
				  , @CODIGOBARRA2 
				  , @CODIGOBARRA3 
				  , @CODIGOBARRA4 
				  , @UMCB1 
				  , @UMCB2 
				  , @UMCB3 
				  , @UMCB4 
				  , @PRECIO8 
				  , @KILOS 
				  , @M3 
				  --, @SuspendidoV 
				  --, @SuspendidoC 
				  , @Dto1 
				  , @Dto2 
				  , @Dto3 
				  , @Dto4 
				  , @Dto5 
				  , @Rec1 
				  , @Rec2 
				  , @Rec3 
				  , @IdTarifaFlete 
				  , @PorcSeguro 
				  , @Dto6 
				  , @Dto7 
				  , @Dto8 
				  , @Dto9 
				  , @FhUltimoCosto 
				  , @FhDtoDesde 
				  , @FhDtoHasta 
				  , @UsaTalle 
				  , @CodigoArtProveedor 
				  , @TalleDefault 
				  , @ColorDefault 
				  , @SexoDefault 
				  , @InsumosPorPorcentaje 
				  , @EnOferta 
				  , @Espesor 
				  , @Ancho 
				  , @Largo 
				  , @PideMedidas 
				  , @EnComodato 
				  , @Transmision 
				  , @PideEquivalencia 
				  , @ITC 
				  , @FHALTA 
				  , @InsertaObserv 
				  , @NO_CONTROLA_STOCK 
				  , @URL1 
				  , @Ubicacion_Habitual 
				  , @Procedencia 
				  , @PideDescripcionAdicional 
				  FROM V_MA_ARTICULOS WHERE IDARTICULO = @IDARTICULO
				 END TRY
				 BEGIN CATCH
					PRINT @@ERROR				  
					print 'error al INSERTAR '+ @IDARTICULO
				END CATCH
	END
			  
	 FETCH NEXT FROM DataInfo INTO 
				   @IDARTICULO 
				  , @CODIGOBARRA 
				  , @DESCRIPCION  
				  , @IDUNIDAD 
				  , @IDRUBRO 
				  , @IDTIPO 
				  , @USASERIE 
				  , @USALOTE 
				  , @EXENTO 
				  , @NOTAS 
				  --, @PUNTOPEDIDO 
				  , @COSTO 
				  , @IMPUESTOS 
				  , @PRECIO1 
				  , @PRECIO2 
				  , @PRECIO3 
				  , @PRECIO4 
				  , @PRECIO5 
				  --, @SUSPENDIDO 
				  --, @PUNTOMAXIMO 
				  , @PoliticaPrecios 
				  , @TasaIVA 
				  , @Moneda 
				  , @RutaImagen 
				  , @IdPercepcion 
				  , @Usuario 
				  , @Observaciones 
				  , @ActualizaCosto 
				  , @IdFamilia 
				  , @UD_CPRA 
				  , @UD_STOCK 
				  , @UD_TTE 
				  , @Presentacion 
				  , @DescripAbrev 
				  , @CostoInsumos 
				  , @DesdeTrigger 
				  , @CodigoBarraDun 
				  , @Perecedero 
				  , @Pesable 
				  , @PRECIO6 
				  , @PRECIO7 
				  , @UTILIDAD 
				  , @CODIGOBARRA1 
				  , @CODIGOBARRA2 
				  , @CODIGOBARRA3 
				  , @CODIGOBARRA4 
				  , @UMCB1 
				  , @UMCB2 
				  , @UMCB3 
				  , @UMCB4 
				  , @PRECIO8 
				  , @KILOS 
				  , @M3 
				  --, @SuspendidoV 
				  --, @SuspendidoC 
				  , @Dto1 
				  , @Dto2 
				  , @Dto3 
				  , @Dto4 
				  , @Dto5 
				  , @Rec1 
				  , @Rec2 
				  , @Rec3 
				  , @IdTarifaFlete 
				  , @PorcSeguro 
				  , @Dto6 
				  , @Dto7 
				  , @Dto8 
				  , @Dto9 
				  , @FhUltimoCosto 
				  , @FhDtoDesde 
				  , @FhDtoHasta 
				  , @UsaTalle 
				  , @CodigoArtProveedor 
				  , @TalleDefault 
				  , @ColorDefault 
				  , @SexoDefault 
				  , @InsumosPorPorcentaje 
				  , @EnOferta 
				  , @Espesor 
				  , @Ancho 
				  , @Largo 
				  , @PideMedidas 
				  , @EnComodato 
				  , @Transmision 
				  , @PideEquivalencia 
				  , @ITC 
				  , @FHALTA 
				  , @InsertaObserv 
				  , @NO_CONTROLA_STOCK 
				  , @URL1 
				  , @Ubicacion_Habitual 
				  , @Procedencia 
				  , @PideDescripcionAdicional 
			END
 
CLOSE  DataInfo
DEALLOCATE  DataInfo
