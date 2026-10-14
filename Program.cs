using CertificadosDownloader.Services;

using var cancellationSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationSource.Cancel();
};

try
{
    DotEnvLoader.CargarArchivoLocal();
    var connectionString = SqliteDatabaseOptions.DesdeEntorno();
    var sevOptions = SevEndpointOptions.DesdeEntorno();
    using var handler = new HttpClientHandler { AllowAutoRedirect = false };
    using var httpClient = new HttpClient(handler) { Timeout = sevOptions.Timeout };
    var repository = new CatalogRepository(connectionString);
    var catalogClient = new SevCatalogClient(httpClient, sevOptions);
    var catalogService = new CatalogSynchronizationService(catalogClient, repository);
    const string actor = "local";
    while (!cancellationSource.IsCancellationRequested)
    {
        Console.WriteLine();
        Console.WriteLine("1. Descargar certificados desde Excel");
        Console.WriteLine("2. Administrar catalogos SEV");
        Console.WriteLine("0. Salir");
        Console.Write("Opcion: ");
        var opcion = Console.ReadLine();
        if (opcion is null)
        {
            return;
        }

        switch (opcion.Trim())
        {
            case "1":
                await DescargarCertificadosAsync(connectionString, sevOptions, httpClient, cancellationSource.Token);
                break;
            case "2":
                var administracion = new CatalogAdministrationConsole(catalogService, repository);
                await administracion.EjecutarAsync(actor, cancellationSource.Token);
                break;
            case "0":
                return;
            default:
                Console.WriteLine("Opcion no valida.");
                break;
        }
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operacion cancelada.");
}
catch (Exception ex)
{
    Console.WriteLine(SafeErrorMessages.ParaUsuario(ex));
    Environment.ExitCode = 1;
}

static async Task DescargarCertificadosAsync(string connectionString, SevEndpointOptions sevOptions, HttpClient httpClient, CancellationToken cancellationToken)
{
    Console.WriteLine("Ruta del archivo Excel (ej. datos.xlsx):");
    var rutaExcel = Console.ReadLine()?.Trim();
    while (string.IsNullOrWhiteSpace(rutaExcel) || !File.Exists(rutaExcel))
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (rutaExcel is null) return;
        Console.WriteLine("Archivo no valido. Intenta de nuevo:");
        rutaExcel = Console.ReadLine()?.Trim();
    }

    Console.WriteLine("Carpeta de salida (ej. Descargas):");
    var carpetaDestino = Console.ReadLine()?.Trim();
    while (string.IsNullOrWhiteSpace(carpetaDestino))
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (carpetaDestino is null) return;
        Console.WriteLine("Ruta no valida. Intenta de nuevo:");
        carpetaDestino = Console.ReadLine()?.Trim();
    }

    Directory.CreateDirectory(carpetaDestino);
    await new CatalogRepository(connectionString).EnsureSchemaAsync(cancellationToken);
    var processor = new ExcelProcessor(connectionString);
    var registros = processor.LeerExcelYResolverIDs(rutaExcel);
    if (registros.Count == 0)
    {
        Console.WriteLine("No se encontraron registros validos. Sincroniza primero los catalogos SEV.");
        return;
    }

    var downloader = new CertificadoDownloader(carpetaDestino, sevOptions.ValidacionUri, sevOptions.ImpresionUri, sevOptions.CalificacionesUri, httpClient, sevOptions.NivelesConCalificaciones);
    await downloader.ProcesarAsync(registros, cancellationToken);
}
