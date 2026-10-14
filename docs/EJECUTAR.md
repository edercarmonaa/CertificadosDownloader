# Ejecutar la version distribuida

## Requisitos del equipo destino

- Windows x64.
- Conexion al portal SEV para sincronizar catalogos y descargar documentos.
- Permisos de escritura en la carpeta de la aplicacion y en la carpeta de salida.

No necesitas instalar .NET, el SDK, Visual Studio ni un servidor de base de datos. El paquete incluye el runtime y SQLite.

## Preparar y abrir

1. Extrae el ZIP completo en una carpeta de tu usuario con permisos de escritura. No ejecutes el programa directamente dentro del ZIP.
2. Conserva juntos `CertificadosDownloader.exe`, `e_sqlite3.dll`, `.env.example` e `Iniciar.cmd`.
3. Abre `Iniciar.cmd`. Establece la carpeta de trabajo correcta y, en el primer arranque, crea `.env` a partir del ejemplo publico. Una configuracion existente se conserva.
4. Si necesitas ajustar endpoints o tiempos de espera, edita `.env` con un editor de texto y vuelve a abrir la aplicacion. Las variables del entorno tienen prioridad.

Tambien puedes iniciar desde PowerShell, estando en la carpeta extraida:

```powershell
.\Iniciar.cmd
```

Para ejecutar el EXE directamente, primero configura `.env` y establece el directorio de trabajo:

```powershell
# Solo la primera vez, si no existe .env:
if (-not (Test-Path -LiteralPath .env)) { Copy-Item .env.example .env }
.\CertificadosDownloader.exe
```

## Primer uso

1. Selecciona `2. Administrar catalogos SEV` y despues `3. Sincronizar niveles y ciclos`.
2. La aplicacion crea `data/certificados.db` automaticamente; no se distribuye una base precargada.
3. Prepara un Excel cuya primera hoja tenga encabezados y estas columnas, en orden: folio, descripcion del nivel, anio de inicio y anio de fin.
4. Selecciona `1. Descargar certificados desde Excel`.
5. Introduce la ruta de un Excel existente, sin comillas exteriores. Por ejemplo, `C:\Documentos\lote.xlsx` solo funcionara si ese archivo existe en tu equipo.
6. Indica una carpeta de salida y revisa PDFs, CSV, JSON e incidencias al terminar.

Los folios deben estar autorizados y el nivel y los anios deben coincidir con los catalogos activos. Los niveles con extraccion de calificaciones se configuran en `CERTIFICADOS_NIVELES_CON_CALIFICACIONES`.

## Archivos locales y actualizaciones

- `.env` contiene tu configuracion y `data/` contiene los catalogos locales.
- PDFs, CSV y JSON pueden contener datos personales: protege la carpeta de salida.
- Los errores identifican posiciones del lote procesado; no necesariamente el numero de fila original del Excel.
- Para actualizar, extrae la nueva version en otra carpeta. Si deseas conservar la configuracion y los catalogos, copia tu `.env` y `data/` con la aplicacion cerrada.
- No compartas una carpeta que ya utilizaste para procesar documentos. Distribuye el ZIP original generado por el script.

## Problemas frecuentes

- **Archivo no valido:** comprueba la ruta del Excel y quita las comillas exteriores.
- **No se encontraron registros validos:** sincroniza los catalogos y revisa las cuatro columnas y sus coincidencias.
- **No se puede guardar:** mueve la aplicacion a una carpeta de usuario con permisos de escritura; evita `Program Files`.
- **Falta una biblioteca:** extrae de nuevo todo el ZIP, incluyendo `e_sqlite3.dll`.
- **Error de configuracion o conexion:** revisa `.env`, la conexion y la disponibilidad del portal. Las URLs deben usar HTTPS y no contener credenciales, consulta ni fragmentos.

La validacion PDF comprueba la cabecera del archivo. El servicio SEV y su HTML pueden cambiar; esta herramienta no incorpora autenticacion ni cifrado de los archivos generados.
