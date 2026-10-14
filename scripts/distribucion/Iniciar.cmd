@echo off
setlocal
cd /d "%~dp0"
if not exist ".env" (
    if not exist ".env.example" (
        echo Falta .env.example. Extrae de nuevo el paquete completo.
        pause
        exit /b 1
    )
    copy /Y ".env.example" ".env" >nul
    if errorlevel 1 (
        echo No fue posible crear .env. Usa una carpeta con permisos de escritura.
        pause
        exit /b 1
    )
)
if not exist "CertificadosDownloader.exe" (
    echo Falta el ejecutable. Extrae de nuevo el paquete completo.
    pause
    exit /b 1
)
"CertificadosDownloader.exe"
set "applicationExit=%errorlevel%"
if not "%applicationExit%"=="0" (
    echo La aplicacion termino con un error. Revisa la configuracion y los permisos.
    pause
)
exit /b %applicationExit%
