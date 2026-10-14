[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$packageName = 'CertificadosDownloader-win-x64-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$outputDirectory = Join-Path (Join-Path $projectRoot 'publish') $packageName
$archivePath = $outputDirectory + '.zip'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'Instala el SDK indicado en global.json antes de publicar.'
}

Push-Location $projectRoot
try {
    Write-Host 'Restaurando dependencias bloqueadas...'
    dotnet restore CertificadosDownloader.sln --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'Fallo la restauracion. Comprueba el SDK de global.json y el acceso a NuGet.' }

    Write-Host 'Compilando y ejecutando pruebas...'
    dotnet test CertificadosDownloader.sln -c Release --no-restore -warnaserror
    if ($LASTEXITCODE -ne 0) { throw 'Fallo la compilacion o las pruebas. No se genero el paquete.' }

    Write-Host 'Generando ejecutable para Windows x64 con runtime incluido...'
    dotnet publish CertificadosDownloader.csproj -c Release --no-restore -o $outputDirectory -p:DebugType=None -p:DebugSymbols=false
    if ($LASTEXITCODE -ne 0) { throw 'Fallo la publicacion local. La carpeta incompleta no debe distribuirse.' }

    foreach ($requiredFile in 'CertificadosDownloader.exe', 'e_sqlite3.dll') {
        if (-not (Test-Path -LiteralPath (Join-Path $outputDirectory $requiredFile) -PathType Leaf)) {
            throw "Falta un archivo requerido del paquete: $requiredFile. No distribuyas esta carpeta."
        }
    }

    # Only public example configuration is distributed, never the local .env or data.
    Copy-Item -LiteralPath (Join-Path $projectRoot '.env.example') -Destination $outputDirectory
    Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE') -Destination $outputDirectory
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'distribucion/Iniciar.cmd') -Destination $outputDirectory
    Copy-Item -LiteralPath (Join-Path $projectRoot 'docs/EJECUTAR.md') -Destination (Join-Path $outputDirectory 'LEEME.md')

    Compress-Archive -Path (Join-Path $outputDirectory '*') -DestinationPath $archivePath
    Write-Host ''
    Write-Host "Carpeta lista: $outputDirectory"
    Write-Host "ZIP listo:     $archivePath"
    Write-Host 'Distribuye el ZIP completo. Extraelo y ejecuta Iniciar.cmd.'
}
finally {
    Pop-Location
}
