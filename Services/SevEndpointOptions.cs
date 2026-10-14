namespace CertificadosDownloader.Services;

public sealed class SevEndpointOptions
{
    private SevEndpointOptions(
        Uri baseUri,
        TimeSpan timeout,
        Uri validacionUri,
        Uri impresionUri,
        Uri calificacionesUri,
        Uri nivelesUri,
        Uri ciclosUri,
        IReadOnlySet<int> nivelesConCalificaciones)
    {
        BaseUri = baseUri;
        Timeout = timeout;
        ValidacionUri = validacionUri;
        ImpresionUri = impresionUri;
        CalificacionesUri = calificacionesUri;
        NivelesUri = nivelesUri;
        CiclosUri = ciclosUri;
        NivelesConCalificaciones = nivelesConCalificaciones;
    }

    public Uri BaseUri { get; }
    public TimeSpan Timeout { get; }
    public Uri ValidacionUri { get; }
    public Uri ImpresionUri { get; }
    public Uri CalificacionesUri { get; }
    public Uri NivelesUri { get; }
    public Uri CiclosUri { get; }
    public IReadOnlySet<int> NivelesConCalificaciones { get; }

    public static SevEndpointOptions DesdeEntorno()
    {
        var baseUrl = Environment.GetEnvironmentVariable("CERTIFICADOS_SEV_BASE_URL")?.Trim();
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) ||
            !EsUrlSegura(baseUri))
        {
            throw new InvalidOperationException(
                "CERTIFICADOS_SEV_BASE_URL debe contener una URL HTTPS sin credenciales, consulta ni fragmento.");
        }

        if (!baseUri.AbsoluteUri.EndsWith('/'))
        {
            baseUri = new Uri(baseUri.AbsoluteUri + "/");
        }

        var timeoutSeconds = 60;
        var configuredTimeout = Environment.GetEnvironmentVariable("CERTIFICADOS_SEV_TIMEOUT_SECONDS");
        if (!string.IsNullOrWhiteSpace(configuredTimeout) &&
            (!int.TryParse(configuredTimeout, out timeoutSeconds) || timeoutSeconds is < 1 or > 300))
        {
            throw new InvalidOperationException(
                "CERTIFICADOS_SEV_TIMEOUT_SECONDS debe ser un entero entre 1 y 300.");
        }

        return new SevEndpointOptions(
            baseUri,
            TimeSpan.FromSeconds(timeoutSeconds),
            ObtenerUrlConfigurada("CERTIFICADOS_URL_VALIDACION", new Uri(baseUri, "Index/validaDatos")),
            ObtenerUrlConfigurada("CERTIFICADOS_URL_IMPRESION", new Uri(baseUri, "Index/imprimirCertificado")),
            ObtenerUrlConfigurada("CERTIFICADOS_URL_CALIFICACIONES", new Uri(baseUri, "scripts/aplicacion/plantillas/Calificaciones.aspx")),
            ObtenerUrlConfigurada("CERTIFICADOS_URL_NIVELES_EDUCATIVOS", new Uri(baseUri, "Index/obtenerNivelesEducativos")),
            ObtenerUrlConfigurada("CERTIFICADOS_URL_CICLOS_ESCOLARES", new Uri(baseUri, "Index/obtenerCiclosEscolares")),
            ObtenerNivelesConCalificaciones());
    }

    public static SevEndpointOptions Crear(Uri baseUri, TimeSpan timeout)
    {
        if (!baseUri.IsAbsoluteUri ||
            !EsUrlSegura(baseUri))
        {
            throw new ArgumentException("La URL base debe ser HTTPS sin credenciales, consulta ni fragmento.", nameof(baseUri));
        }

        var normalizada = baseUri.AbsoluteUri.EndsWith('/') ? baseUri : new Uri(baseUri.AbsoluteUri + "/");
        return new SevEndpointOptions(
            normalizada,
            timeout,
            new Uri(normalizada, "Index/validaDatos"),
            new Uri(normalizada, "Index/imprimirCertificado"),
            new Uri(normalizada, "scripts/aplicacion/plantillas/Calificaciones.aspx"),
            new Uri(normalizada, "Index/obtenerNivelesEducativos"),
            new Uri(normalizada, "Index/obtenerCiclosEscolares"),
            new HashSet<int> { 4, 5 });
    }

    private static Uri ObtenerUrlConfigurada(string variable, Uri valorPredeterminado)
    {
        var valor = Environment.GetEnvironmentVariable(variable)?.Trim();
        if (string.IsNullOrWhiteSpace(valor))
        {
            return valorPredeterminado;
        }

        if (!Uri.TryCreate(valor, UriKind.Absolute, out var uri) ||
            !EsUrlSegura(uri))
        {
            throw new InvalidOperationException($"{variable} debe contener una URL HTTPS sin credenciales, consulta ni fragmento.");
        }

        return uri;
    }

    private static bool EsUrlSegura(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo) &&
        string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);

    private static IReadOnlySet<int> ObtenerNivelesConCalificaciones()
    {
        var configuracion = Environment.GetEnvironmentVariable("CERTIFICADOS_NIVELES_CON_CALIFICACIONES")?.Trim();
        if (string.IsNullOrWhiteSpace(configuracion))
        {
            return new HashSet<int> { 4, 5 };
        }

        var niveles = new HashSet<int>();
        foreach (var valor in configuracion.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!int.TryParse(valor, out var nivel) || nivel <= 0)
            {
                throw new InvalidOperationException("CERTIFICADOS_NIVELES_CON_CALIFICACIONES debe contener identificadores positivos separados por comas.");
            }

            niveles.Add(nivel);
        }

        if (niveles.Count == 0)
        {
            throw new InvalidOperationException("CERTIFICADOS_NIVELES_CON_CALIFICACIONES debe contener al menos un identificador.");
        }

        return niveles;
    }
}
