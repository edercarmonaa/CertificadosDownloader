using System.Net.Http.Headers;
using CertificadosDownloader.Models;

namespace CertificadosDownloader.Services;

public sealed class SevCatalogClient : ISevCatalogClient
{
    private readonly HttpClient _httpClient;
    private readonly SevEndpointOptions _options;

    public SevCatalogClient(HttpClient httpClient, SevEndpointOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<RespuestaSevCatalogo<SevNivelEducativo>> ConsultarNivelesAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.NivelesUri);
        return await EnviarAsync(request, SevCatalogResponseParser.LeerNiveles, cancellationToken);
    }

    public async Task<RespuestaSevCatalogo<SevCicloEscolar>> ConsultarCiclosAsync(
        int nivelEducativoId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.CiclosUri)
        {
            Content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("nivelEducativoId", nivelEducativoId.ToString())
            ])
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        return await EnviarAsync(request, SevCatalogResponseParser.LeerCiclos, cancellationToken);
    }

    private async Task<RespuestaSevCatalogo<T>> EnviarAsync<T>(
        HttpRequestMessage request,
        Func<string, RespuestaSevCatalogo<T>> lector,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var contenido = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Error<T>($"SEV respondio HTTP {(int)response.StatusCode}.");
            }

            return lector(contenido);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Error<T>($"La consulta a SEV excedio el tiempo de espera de {_options.Timeout.TotalSeconds:0} segundos.");
        }
        catch (HttpRequestException ex)
        {
            return Error<T>(SafeErrorMessages.ParaUsuario(ex));
        }
    }

    private static RespuestaSevCatalogo<T> Error<T>(string advertencia)
    {
        var resultado = new RespuestaSevCatalogo<T>
        {
            EsValida = false,
            EstaVacia = false,
            Registros = new List<T>()
        };
        resultado.Advertencias.Add(advertencia);
        return resultado;
    }
}
