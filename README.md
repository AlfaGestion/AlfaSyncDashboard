# Alfa Sync Dashboard

Proyecto WinForms para controlar la sincronización de datos entre la base central y los puntos de venta.

## Qué hace esta versión

- Carga los locales desde `dbo.V_TA_TPV`
- Permite probar conexiones a cada local usando `SERVER / DBNAME / USUARIO / PASSWORD`
- Permite analizar diferencias entre central y local
- Incluye control de precios por lista
- Guarda historial en `dbo.LOG_SYNC`
- Permite automatizar la ejecución con tarea programada
- Evita doble ejecución simultánea en la misma máquina
- Sincroniza todas las etapas por conexión directa al local

## Modos de sincronización

### `Enviar precios y costos`

Ejecuta:

1. `Artículos`
2. `PreciosCab`
3. `Precios`

### `Enviar todo`

Ejecuta:

1. `Categorías artículo`
2. `Rubros`
3. `Unidades`
4. `Tipos artículo`
5. `Familias`
6. `Artículos`
7. `PreciosCab`
8. `Precios`

## Lógica de ejecución

La sincronización trabaja por conexión directa:

- lee los datos desde central
- crea una tabla temporal en el local
- copia los datos en bloque
- hace `UPDATE` solo sobre filas distintas
- inserta las filas faltantes

No depende de linked servers ni del contenido de archivos `.sql`.

## Reglas especiales actualmente aplicadas

- `V_MA_PRECIOSCAB` trabaja con `TipoLista = 'V'`
- `V_MA_PRECIOS` trabaja con `TipoLista = 'V'`
- `V_MA_PRECIOS` valida existencia por `IdLista + IdArticulo`

## Configuración

Editar `AlfaSyncDashboard/appsettings.json`.

Campos principales:

```json
"DefaultScriptsPath": "C:\\dev\\AlfaSyncDashboard\\Scripts",
"CentralConnectionString": "...",
"DisableDestinationTriggersDuringSync": true,
"SelectedLocalCodes": []
```

`DefaultScriptsPath` se mantiene por compatibilidad de configuración, pero la sincronización directa actual no depende de archivos `.sql`.

### `ScriptSets`

Cada `ScriptSet` define la secuencia lógica de etapas. La app mantiene los nombres por compatibilidad, pero la ejecución real es directa y no depende de los archivos `.sql`.

Ejemplo:

```json
"DEFAULT": {
  "FamiliesScript": "ACTUALIZA_FAMILIAS.SQL",
  "CategoriesScript": "ACTUALIZA_CATEGORIAS_ARTICULO.SQL",
  "RubrosScript": "ACTUALIZA_RUBROS.SQL",
  "UnitsScript": "ACTUALIZA_UNIDADES.SQL",
  "ArticleTypesScript": "ACTUALIZA_TIPOS_ARTICULO.SQL",
  "ArticlesScript": "ACTUALIZA_ARTICULOS.SQL",
  "PriceCabScript": "ACTUALIZA_V_MA_PRECIOSCAB.SQL",
  "PricesScript": "ACTUALIZA_V_MA_PRECIOS.SQL"
}
```

## Selección de locales

- los locales marcados en `Sel` se guardan en `appsettings.json`
- esa selección se reutiliza al abrir la app
- la tarea programada usa esa misma selección

## Automatización

La app soporta ejecución por consola:

```powershell
AlfaSyncDashboard.exe --sync prices
AlfaSyncDashboard.exe --sync full
```

También puede crear o actualizar una tarea programada desde la pantalla `Historial y tarea`.

## Protección contra doble ejecución

La app usa un bloqueo exclusivo local para impedir dos sincronizaciones simultáneas en la misma máquina, incluso con múltiples usuarios por escritorio remoto.

## Instalación

El setup:

- no pisa `appsettings.json` si ya existe
- ya no necesita copiar scripts SQL para la sincronización

## Requisitos

- Windows
- Visual Studio 2022 o superior
- .NET 8 SDK
- Acceso a SQL Server central
- Acceso a SQL Server de los locales
- Inno Setup 6 para generar instalador

## Cómo generar el setup

```powershell
powershell -ExecutionPolicy Bypass -File .\Setup\Build-Setup.ps1
```

El instalador queda en:

- `Setup\Output\`

## Tabla de log

La app crea automáticamente:

```sql
IF OBJECT_ID('dbo.LOG_SYNC', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LOG_SYNC
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Fecha DATETIME NOT NULL DEFAULT GETDATE(),
        Local NVARCHAR(100) NOT NULL,
        Proceso NVARCHAR(100) NOT NULL,
        Mensaje NVARCHAR(MAX) NULL,
        Estado NVARCHAR(20) NOT NULL
    );
END
```

## Documentación de uso

Ver también:

- [docs/INSTRUCTIVO_USUARIO.md](C:/dev/AlfaSyncDashboard/docs/INSTRUCTIVO_USUARIO.md)
