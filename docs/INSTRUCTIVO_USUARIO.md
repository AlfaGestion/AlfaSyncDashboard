# Instructivo de Uso

## Alfa Sync Dashboard

Alfa Sync Dashboard es una aplicación para controlar la sincronización de datos entre la base central y los puntos de venta.

La app permite:

- cargar los locales desde la base central
- probar la conexión a cada punto de venta
- analizar diferencias entre central y local
- controlar precios por lista
- consultar historial de sincronizaciones
- crear o actualizar una tarea programada para automatizar la ejecución
- enviar precios y costos
- enviar todo el conjunto de datos definido para la sincronización
- ver el estado y el log del proceso en tiempo real

## Antes de usar

Antes de comenzar, verificar:

- que la conexión a la base central esté bien configurada
- que cada punto de venta tenga correctamente cargado `Server`, `Base`, `Usuario` y `Password`
- que la aplicación pueda conectarse tanto a la base central como a las bases locales

## Pantalla principal

En la grilla principal se muestran los locales disponibles y su información:

- `Sel`: permite marcar uno o más locales para trabajar
- `Código`: código del punto de venta
- `Sucursal`: número o identificador de sucursal
- `Descripción`: nombre visible del local
- `Server`: servidor SQL del local
- `Base`: base de datos del local
- `ScriptSet`: grupo lógico de sincronización asignado
- `Conexión`: resultado de la prueba de conexión
- `Última sync`: fecha y hora de la última sincronización exitosa
- `Estado`: estado actual del local dentro de la app

En la parte inferior:

- la barra de estado muestra el avance general
- el área de log muestra mensajes del proceso en tiempo real

Importante:

- los locales marcados en `Sel` se guardan en la configuración
- al volver a abrir la app, se vuelven a marcar automáticamente
- esa misma selección es la que usa el modo automático por consola o tarea programada

## Qué hace cada botón

### `Actualizar locales`

Vuelve a leer los locales desde la vista central `V_TA_TPV`.

Usar este botón cuando:

- se agregaron nuevos locales
- se modificó algún dato de conexión
- se quiere refrescar la grilla

### `Configuración`

Abre la pantalla de configuración general de la aplicación.

Desde ahí se revisa:

- la conexión a la base central

Si se cambia la configuración, conviene cerrar y volver a abrir la app para asegurar que todo quede recargado correctamente.

### `Probar conexiones`

Intenta conectarse a cada local usando los datos de `Server`, `Base`, `Usuario` y `Password` del punto de venta.

Resultado esperado:

- `OK` si la conexión al local funciona
- `ERROR` si la conexión falla

Este paso no sincroniza nada. Solo valida conectividad.

### `Analizar seleccionados`

Compara los locales seleccionados contra la base central.

El análisis revisa diferencias en:

- familias
- artículos
- cabeceras de listas de precios
- precios

Sirve para saber si el local está alineado con central antes de enviar datos.

Este botón no modifica información. Solo informa diferencias.

### `Control precios`

Permite comparar costos o precios por lista entre central y uno o más locales seleccionados.

En modo precios:

- la app trabaja con `TipoLista = 'V'`
- solo muestra listas de ventas
- compara los precios del central contra cada local seleccionado

### `Historial y tarea`

Abre una pantalla con dos funciones:

- ver el historial de `LOG_SYNC`
- crear, actualizar, consultar o borrar la tarea programada de sincronización automática

Desde esa pantalla se puede:

- elegir cada cuántos minutos ejecutar
- elegir si la tarea corre `Precios y costos` o `Enviar todo`
- copiar el comando `schtasks`
- ver la definición actual de la tarea en Windows

### `Enviar precios y costos`

Sincroniza únicamente:

- artículos
- cabeceras de precios
- precios

Es la opción recomendada cuando se quiere actualizar precios y costos sin enviar tablas maestras.

Orden de ejecución:

1. `Artículos`
2. `PreciosCab`
3. `Precios`

### `Enviar todo`

Sincroniza:

- categorías de artículo
- rubros
- unidades
- tipos de artículo
- familias
- artículos
- cabeceras de precios
- precios

Usar esta opción cuando se necesita una actualización completa del local.

Orden de ejecución:

1. `Categorías artículo`
2. `Rubros`
3. `Unidades`
4. `Tipos artículo`
5. `Familias`
6. `Artículos`
7. `PreciosCab`
8. `Precios`

### `Cancelar`

Solicita detener el proceso actual.

Si la sincronización está en curso, la app intentará cancelar en el punto más seguro posible.

## Cómo se ejecuta la sincronización

La sincronización trabaja por conexión directa al local.

La app:

- lee los datos desde central
- crea una tabla temporal en el local
- copia los datos en bloque
- actualiza solo las filas distintas
- inserta las filas faltantes

Esto prioriza velocidad y evita depender de linked servers o del contenido de archivos `.sql`.

## Cómo se actualizan los scripts al instalar

El instalador:

- no pisa `appsettings.json` si ya existe
- no necesita copiar scripts SQL para que la sincronización funcione

## Flujo recomendado de uso

Para minimizar errores, se recomienda este orden:

1. Abrir la app.
2. Hacer clic en `Actualizar locales`.
3. Hacer clic en `Probar conexiones`.
4. Marcar un solo local en `Sel`.
5. Hacer clic en `Analizar seleccionados`.
6. Si el análisis es correcto, hacer clic en `Enviar precios y costos`.
7. Revisar el log y el estado final.
8. Recién después repetir con más locales o usar `Enviar todo` si corresponde.

## Recomendación para primer uso

La primera vez conviene:

- trabajar con un solo local
- probar primero `Enviar precios y costos`
- validar el resultado en el local
- luego ampliar al resto

## Cómo interpretar el log

En la parte inferior de la ventana se muestran mensajes con hora.

Ejemplos de uso del log:

- confirmar que un local fue tomado por el proceso
- ver en qué etapa está trabajando
- detectar errores de conexión o de actualización
- confirmar cuántas filas fueron procesadas
- confirmar en qué etapa directa ocurrió un problema

## Historial de sincronización

La pantalla `Historial y tarea` permite ver los registros guardados en `dbo.LOG_SYNC`.

Por defecto se consulta el último mes, pero el período se puede ajustar.

El historial sirve para:

- verificar qué locales se sincronizaron
- ver errores anteriores
- revisar horarios y estados
- confirmar ejecuciones automáticas

## Estados habituales

Algunos estados que puede mostrar la app:

- `Listo`: el local está cargado y sin proceso en curso
- `Pendiente`: todavía no se probó la conexión
- `Sincronizando...`: se está ejecutando una sincronización
- `OK`: el proceso terminó correctamente
- `ERROR`: ocurrió un problema durante la conexión, análisis o sincronización
- `Cancelado`: el usuario canceló el proceso

## Automatización por tarea programada

La app permite ejecutar sincronización automática mediante tarea programada de Windows.

La tarea usa el mismo ejecutable y los locales marcados en `Sel`.

Modos disponibles:

- `--sync prices`
- `--sync full`

La pantalla `Historial y tarea` puede crear la tarea automáticamente.

También se puede hacer manualmente con `schtasks`.

La tarea toma:

- la configuración guardada
- la selección de locales marcada en la app
- la ruta instalada del ejecutable

## Protección contra doble ejecución

La app evita que se ejecuten dos sincronizaciones al mismo tiempo en la misma máquina.

Esto aplica incluso si:

- hay dos usuarios conectados por escritorio remoto
- un usuario abre la app manualmente mientras corre la tarea programada

Si ya hay una ejecución activa, la siguiente no arranca.

## Buenas prácticas

- no ejecutar sincronizaciones masivas sin antes probar con un local
- revisar siempre `Conexión` antes de enviar datos
- usar `Analizar seleccionados` antes de sincronizar si hay dudas
- no cerrar la app mientras una sincronización esté en curso
- revisar el log ante cualquier error
- si se automatiza, validar el historial luego de la primera ejecución programada

## Qué no hace cada acción

- `Probar conexiones` no actualiza datos
- `Analizar seleccionados` no modifica datos
- `Cancelar` no borra información ya sincronizada

## Soporte ante errores

Si aparece un error:

1. revisar el mensaje exacto en el log
2. verificar los datos de conexión del local
3. probar acceso manual a la base del local
4. volver a ejecutar con un solo local seleccionado

Si el error persiste, registrar:

- nombre del local
- hora del error
- botón utilizado
- mensaje completo mostrado en el log
- nombre de la etapa donde ocurrió el problema
