# CertificadosDownloader

Descarga por lotes certificados educativos del portal de certificación electrónica de la Secretaría de Educación de Veracruz (SEV) a partir de un Excel. Resuelve niveles y ciclos con catálogos locales y guarda las calificaciones disponibles en CSV.

## El problema

Cuando una persona debe reunir varios certificados, consultar cada folio, seleccionar su nivel y ciclo, descargar el PDF y copiar las calificaciones implica repetir el mismo trabajo por cada registro. Una selección incorrecta o una respuesta fallida puede dejar documentos pendientes y obligar a repetir la consulta.

El proyecto automatiza ese recorrido para personas que ya disponen de los folios y están autorizadas a consultar los documentos. Sin la herramienta, ese lote se tendría que procesar mediante consultas individuales al portal.

## La solución

Primero sincroniza los niveles educativos y ciclos escolares de SEV en SQLite. Después lee la primera hoja del Excel, busca los identificadores correspondientes en los catálogos activos y consulta cada registro de forma secuencial. Guarda los PDFs con cabecera válida, extrae las calificaciones de los niveles configurados y produce un JSON consolidado y un registro de incidencias.

## Funcionalidades principales

- Sincronización completa o por nivel de catálogos mediante solicitudes POST.
- Consulta y activación/desactivación de niveles y ciclos desde un menú de consola.
- Persistencia transaccional de catálogos y auditoría administrativa en SQLite.
- Lectura de Excel con omisión de filas inválidas o sin coincidencia en catálogos activos.
- Descarga de PDFs con comprobación de la cabecera `%PDF-` y nombres de archivo saneados.
- Extracción de hasta seis tablas semestrales a CSV, con escape de campos y protección frente a fórmulas de hojas de cálculo.
- Reintento de calificaciones cuando el PDF ya existe, sin descargarlo de nuevo.
- Cancelación con Ctrl+C y errores por posición del registro, sin imprimir folios ni mensajes externos sin filtrar.

## ¿Qué mejora este proyecto?

Reduce las consultas repetitivas, reutiliza la correspondencia entre descripciones e identificadores y permite continuar con otros registros cuando una consulta falla. Los archivos quedan organizados en una carpeta de salida y las incidencias permiten identificar qué posición del lote debe revisarse.

## ¿Para quién está pensado?

Para personas encargadas de reunir certificados y calificaciones a partir de listas de folios autorizados. La misma persona administra los catálogos y ejecuta las descargas; el código no define roles, cuentas ni permisos de usuario.

## Tecnologías utilizadas

| Tecnología | Uso | Versión declarada |
| --- | --- | --- |
| C# / .NET | Aplicación de consola | `net9.0` |
| ClosedXML | Lectura de Excel | `0.105.0` |
| HtmlAgilityPack | Lectura de tablas HTML | `1.12.1` |
| Microsoft.Data.Sqlite | Acceso a SQLite | `9.0.5` |
| SQLitePCLRaw.bundle_e_sqlite3 | Motor SQLite nativo actualizado | `3.0.2` |
| xUnit / Microsoft.NET.Test.Sdk | Pruebas automatizadas | `2.9.3` / `17.12.0` |

Las versiones transitivas y sus hashes quedan registrados en los dos archivos `packages.lock.json`. La biblioteca nativa se obtiene a través de `SourceGear.sqlite3`.

## Requisitos

- Windows x64: es el destino configurado en el proyecto.
- .NET SDK **9.0.319**, fijado en `global.json` para reproducir la compilación.
- Acceso a `nuget.org` para restaurar dependencias.
- Conexión al portal SEV para sincronizar y descargar; las pruebas usan respuestas simuladas.
- Un Excel `.xlsx` autorizado y una carpeta local con permisos de escritura.

## Instalación

Todavía no hay una URL de repositorio público configurada. Sustituye el valor de la primera línea por la URL que el responsable publique posteriormente:

```powershell
$repositorio = 'URL_DEL_REPOSITORIO'
git clone $repositorio CertificadosDownloader
cd CertificadosDownloader
Copy-Item .env.example .env
dotnet restore CertificadosDownloader.sln --locked-mode
dotnet build CertificadosDownloader.sln -c Release --no-restore
dotnet test CertificadosDownloader.sln -c Release --no-build --no-restore
```

Si ya tienes `.env`, conserva tu configuración local. `NuGet.Config` utiliza exclusivamente el origen público de NuGet. No se requieren credenciales de base de datos ni claves de API.

## Configuración

La aplicación busca `.env` en el directorio de trabajo y, si no existe allí, junto al ejecutable. Las variables del proceso tienen prioridad sobre el archivo. No se requiere instalar una biblioteca adicional para cargarlo.

| Variable | Función | Ejemplo o valor predeterminado |
| --- | --- | --- |
| `CERTIFICADOS_SQLITE_PATH` | Archivo SQLite; una ruta relativa se resuelve desde el directorio de trabajo. | `data/certificados.db` |
| `CERTIFICADOS_SEV_BASE_URL` | URL base HTTPS del servicio; obligatoria. | `https://certificacion-electronica.sev.gob.mx/` |
| `CERTIFICADOS_SEV_TIMEOUT_SECONDS` | Tiempo máximo por solicitud, entre 1 y 300 segundos. | `60` |
| `CERTIFICADOS_URL_VALIDACION` | Sustitución opcional del endpoint de validación. | Vacío: `Index/validaDatos` desde la base |
| `CERTIFICADOS_URL_IMPRESION` | Sustitución opcional del endpoint de impresión. | Vacío: `Index/imprimirCertificado` |
| `CERTIFICADOS_URL_CALIFICACIONES` | Sustitución opcional del endpoint de calificaciones. | Vacío: `scripts/aplicacion/plantillas/Calificaciones.aspx` |
| `CERTIFICADOS_URL_NIVELES_EDUCATIVOS` | Sustitución opcional del endpoint de niveles. | Vacío: `Index/obtenerNivelesEducativos` |
| `CERTIFICADOS_URL_CICLOS_ESCOLARES` | Sustitución opcional del endpoint de ciclos. | Vacío: `Index/obtenerCiclosEscolares` |
| `CERTIFICADOS_NIVELES_CON_CALIFICACIONES` | IDs positivos separados por comas. | `4,5` |

Los endpoints opcionales requieren URLs absolutas HTTPS. Las URLs no aceptan credenciales, parámetros de consulta ni fragmentos. Configura únicamente servicios de confianza: los folios se envían al endpoint seleccionado. El cliente de consola no sigue redirecciones automáticamente.

## Base de datos

SQLite almacena los catálogos y la auditoría administrativa, no los PDFs ni las calificaciones. La aplicación crea el directorio y las tablas automáticamente al consultar o sincronizar los catálogos. No necesita un servidor de base de datos.

- `SevNivelesEducativos`: identificador, descripción, estado y última sincronización.
- `SevCiclosEscolares`: ciclos por nivel, años, estado y última sincronización; usa clave compuesta.
- `SevCatalogAudit`: acciones, fechas, conteos, resultados y advertencias. La consola usa el actor genérico `local`.

El esquema está en [Database/001_CreateSevCatalogs.sql](Database/001_CreateSevCatalogs.sql) y también se crea desde el repositorio de datos. No hay semillas ni un sistema de migraciones versionadas: llena los catálogos desde el menú de sincronización. Una respuesta vacía, inválida o fallida conserva los datos previos; una respuesta válida actualiza registros y desactiva los que ya no aparecen en ese catálogo.

## Ejecutar el proyecto

Desde la raíz del proyecto:

```powershell
dotnet run --project CertificadosDownloader.csproj -c Release --no-build --no-restore
```

## Publicar una version distribuible

Desde la raiz del proyecto, ejecuta el script con PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Publicar.ps1
```

Requiere el SDK de `global.json` y acceso a NuGet. La opcion `ExecutionPolicy Bypass` se limita al proceso del comando; no cambia la politica de PowerShell de forma permanente.

El script restaura con `--locked-mode`, compila y ejecuta las pruebas Release, y genera una carpeta nueva de Windows x64 con runtime incluido. Si falla una comprobacion, se detiene. No copia tu `.env`, bases de datos ni documentos procesados.

Al terminar muestra las rutas de la carpeta y del ZIP, ambos dentro de `publish/`, con fecha y un identificador unico. Cada paquete contiene:

```text
CertificadosDownloader.exe
e_sqlite3.dll
.env.example
Iniciar.cmd
LEEME.md
LICENSE
```

Distribuye el ZIP completo generado por el script. `publish/` esta excluido de Git. Este comando solo empaqueta archivos locales; cualquier subida del ZIP o creacion de una release se realiza posteriormente de forma manual.

## Ejecutar la version publicada

En el equipo destino solo se necesita Windows x64, conexion a SEV y permisos de escritura; no es necesario instalar .NET ni el SDK.

1. Extrae el ZIP completo en una carpeta de usuario con permisos de escritura.
2. Abre `Iniciar.cmd`, o ejecuta `.\Iniciar.cmd` desde PowerShell en esa carpeta.
3. En el primer arranque, el lanzador crea `.env` desde `.env.example`. Conserva una configuracion existente y establece la carpeta de trabajo.
4. Sincroniza los catalogos desde la opcion `2`, submenu `3`.
5. Selecciona la opcion `1` e introduce la ruta de tu Excel real, sin comillas exteriores, y la carpeta de salida.

Conserva `e_sqlite3.dll` junto al ejecutable. La base SQLite se crea automaticamente en `data/`; no hace falta distribuir una base precargada. La guia [Ejecutar la version distribuida](docs/EJECUTAR.md) tambien se incluye como `LEEME.md` en cada paquete y explica la configuracion, actualizaciones y problemas frecuentes.

## Uso

### 1. Sincronizar los catálogos

1. Selecciona `2. Administrar catalogos SEV`.
2. Selecciona `3. Sincronizar niveles y ciclos`.
3. Revisa el resultado. Las otras opciones permiten consultar los catálogos, sincronizar un nivel y activar o desactivar registros.

### 2. Preparar el Excel

La primera hoja debe tener una fila de encabezado y estas cuatro columnas, en este orden. Los títulos no se validan; importa su posición.

| Columna | Contenido | Ejemplo ficticio |
| --- | --- | --- |
| 1 | Folio | `FOLIO-EJEMPLO-001` |
| 2 | Descripción del nivel educativo presente en el catálogo activo | `Media Superior` |
| 3 | Año de inicio | `2024` |
| 4 | Año de fin | `2025` |

El ejemplo de folio es ficticio y no sirve para descargar un documento real. La descripción y los años deben coincidir con los catálogos sincronizados. Las filas inválidas se omiten con un aviso que indica su número.

### 3. Descargar

1. Selecciona `1. Descargar certificados desde Excel`.
2. Introduce la ruta del Excel y la carpeta de salida.
3. Revisa los documentos y `errores.log` al finalizar.

```text
carpeta-de-salida/
  <folio-saneado>.pdf
  calificaciones/<folio-saneado>.csv
  certificados_YYYYMMDD_HHMM.json
  errores.log
```

Las calificaciones solo se solicitan para los niveles configurados. El JSON contiene datos de los certificados consultados, incluidos nombres cuando SEV los devuelve. Los PDFs, CSV y JSON son documentos privados de operación y se conservan localmente.

Los números de registro del log corresponden al orden de los registros válidos procesados, después de omitir filas inválidas del Excel; no necesariamente coinciden con su número de fila. Un error de calificaciones no impide conservar un PDF descargado.

## Estructura del proyecto

```text
Program.cs                       Menú y configuración de la consola
Models/                          Contratos y resultados
Services/                        Descargas, Excel, HTML, catálogos y SQLite
Database/001_CreateSevCatalogs.sql Esquema de catálogos
Tests/CertificadosDownloader.Tests/ Pruebas y lockfile de tests
scripts/Publicar.ps1             Generacion local del paquete y ZIP
scripts/distribucion/Iniciar.cmd Lanzador para el equipo destino
docs/EJECUTAR.md                 Guia incluida en el paquete
.env.example                     Configuración pública de ejemplo
packages.lock.json               Dependencias de la aplicación
NuGet.Config                     Origen público de paquetes
global.json                      SDK utilizado
```

No se incluyen capturas de pantalla; pueden agregarse posteriormente usando exclusivamente datos ficticios.

## Seguridad

- Usa la herramienta únicamente con documentos que estés autorizado a procesar.
- Mantén la configuración del entorno en `.env` o variables del proceso; `.env.example` solo contiene configuración pública y valores de ejemplo.
- No subas `.env`, bases SQLite, Excel de entrada, PDFs, CSV, JSON de resultados, logs, respaldos ni claves privadas. `.gitignore` cubre esos archivos y los directorios de compilación.
- El programa filtra excepciones y respuestas externas antes de mostrarlas o registrarlas. Los archivos de salida siguen conteniendo información personal necesaria para su propósito; protégelos con permisos locales adecuados.
- Git no aplica reglas de exclusión a archivos ya rastreados y permite saltarlas con `git add -f`. Revisa siempre lo que vas a incluir en un commit.
- No incluyas tokens o contraseñas dentro de URLs, ejemplos o incidencias públicas.
- Si detectas una vulnerabilidad, contacta de forma privada al responsable del repositorio. Aún no se ha definido un canal de contacto de seguridad; evita adjuntar documentos o credenciales a una incidencia pública.

## Pruebas

```powershell
dotnet restore CertificadosDownloader.sln --locked-mode
dotnet test CertificadosDownloader.sln -c Release --no-restore
dotnet list CertificadosDownloader.sln package --vulnerable --include-transitive
```

La suite comprueba contratos HTTP simulados, parseo de catálogos, SQLite, filas Excel inválidas, cabeceras PDF, CSV, reintentos de calificaciones, URLs seguras, filtrado de logs y la versión del motor SQLite. Crea datos ficticios y archivos temporales, y no consulta folios reales.

No hay un linter independiente configurado. La compilación ejecuta los analizadores incluidos en el SDK.

## Estado del proyecto

Versión inicial funcional, con pruebas automatizadas de los flujos locales y del contrato HTTP simulado. La compatibilidad con respuestas actuales del portal debe comprobarse con una consulta autorizada antes de utilizarla para un lote real.

## Limitaciones actuales

- Solo hay interfaz de consola y un destino configurado de Windows x64.
- Depende de la disponibilidad, horarios y formato de respuestas de SEV; no incluye reintentos automáticos por fallo de red.
- La comprobación del PDF revisa su cabecera; no valida su firma, autenticidad ni estructura completa.
- El extractor depende de los elementos HTML `A1` a `A6` y `grid1` a `grid6`; una respuesta sin esas tablas puede generar un CSV con solo encabezados.
- Los archivos existentes se reconocen por nombre, sin verificar su integridad. Dos folios que produzcan el mismo nombre saneado pueden colisionar.
- El JSON usa una marca de tiempo con precisión de minutos; dos lotes en la misma carpeta durante el mismo minuto pueden sobrescribirlo.
- No incorpora autenticación, cifrado de la base de datos ni controles de acceso a los archivos generados.
- No sigue redirecciones HTTP. Si SEV cambia sus URLs, revisa la configuración.

## Próximas mejoras

Posibles mejoras, todavía no implementadas ni comprometidas:

- Detectar respuestas de calificaciones vacías o incompatibles antes de generar un CSV.
- Evitar colisiones de nombres y conservar resultados de varios lotes en el mismo minuto.
- Añadir reintentos con espera y un resumen de pendientes por lote.
- Definir un canal privado para reportes de seguridad.

## Contribuciones

Cuando exista el repositorio público, crea un fork y una rama para el cambio. Mantén los cambios acotados, utiliza fixtures ficticios y ejecuta la restauración bloqueada, el build y las pruebas antes de enviar un pull request. Explica qué comportamiento cambia y cómo se verificó. No adjuntes Excel, certificados, bases de datos ni logs de personas reales.

Si modificas dependencias de forma justificada, regenera ambos lockfiles con `dotnet restore CertificadosDownloader.sln --force-evaluate` y comprueba de nuevo vulnerabilidades y compatibilidad.

## Licencia

Este proyecto se distribuye bajo la [licencia MIT](LICENSE).
