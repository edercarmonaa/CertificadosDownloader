using System.Net;
using System.Text;
using CertificadosDownloader.Services;
using Xunit;

namespace CertificadosDownloader.Tests;

public sealed class SevCatalogClientTests
{
    [Fact]
    public async Task ConsultarCiclos_UsaPostYEnviaNivelComoFormulario()
    {
        var handler = new StubHandler(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/Index/obtenerCiclosEscolares", request.RequestUri!.AbsolutePath);
            Assert.Equal("nivelEducativoId=5", await request.Content!.ReadAsStringAsync());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[{\"cicloEscolarId\":1,\"descripcion\":\"2024 - 2025\"}]", Encoding.UTF8, "application/json")
            };
        });
        using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        var options = SevEndpointOptions.Crear(new Uri("https://sev.example/"), TimeSpan.FromSeconds(5));
        var client = new SevCatalogClient(httpClient, options);

        var respuesta = await client.ConsultarCiclosAsync(5, CancellationToken.None);

        Assert.True(respuesta.EsValida);
        Assert.Single(respuesta.Registros);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => responder(request);
    }
}
