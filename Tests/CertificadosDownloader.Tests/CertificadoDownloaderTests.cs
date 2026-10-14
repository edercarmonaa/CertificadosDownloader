using System.Net;
using System.Text;
using CertificadosDownloader.Models;
using CertificadosDownloader.Services;
using Xunit;

namespace CertificadosDownloader.Tests;

public sealed class CertificadoDownloaderTests
{
    [Fact]
    public async Task ProcesarAsync_NoGuardaComoPdfUnaRespuestaHtml()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"certificados-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        try
        {
            var handler = new StubHandler(request =>
            {
                if (request.Method == HttpMethod.Post)
                {
                    Assert.Contains("folio=F%26X%3D1", request.RequestUri!.Query, StringComparison.Ordinal);
                    return Json("""
                        { "exito": true, "respuesta": {
                          "certificadoId": 12, "folio": "../folio", "cicloEscolar": "2024 - 2025",
                          "nivelEducativoId": 4, "anioInicio": 2024, "anioFin": 2025
                        } }
                        """);
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("<html>Error temporal</html>", Encoding.UTF8, "text/html")
                };
            });
            using var client = new HttpClient(handler);
            var downloader = new CertificadoDownloader(
                outputDirectory,
                new Uri("https://sev.example/validacion"),
                new Uri("https://sev.example/impresion"),
                new Uri("https://sev.example/calificaciones"),
                client,
                new HashSet<int> { 4 });

            await downloader.ProcesarAsync(
                [new RegistroEntrada { Folio = "F&X=1", Nivel = "Nivel", Inicio = 2024, Fin = 2025, CicloEscolarId = 8, NivelEducativoId = 4 }],
                CancellationToken.None);

            Assert.Empty(Directory.GetFiles(outputDirectory, "*.pdf"));
            var log = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "errores.log"));
            Assert.Contains("PDF valido", log, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(outputDirectory, true);
        }
    }

    [Theory]
    [InlineData("=SUM(1,1)")]
    [InlineData("&#9;=SUM(1,1)")]
    [InlineData("&#160;=SUM(1,1)")]
    public async Task ExtraerYGuardarCSVAsync_EscapaFormulasDeExcel(string formula)
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"certificados-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        try
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($$"""
                    <a id="A1">Primer semestre</a>
                    <table id="grid1"><tr><th>Materia</th></tr><tr><td>{{formula}}</td><td>10</td><td>2024</td><td>2025</td></tr></table>
                    """, Encoding.UTF8, "text/html")
            });
            using var client = new HttpClient(handler);
            var extractor = new CalificacionExtractor(outputDirectory, new Uri("https://sev.example/calificaciones"), client, new HashSet<int> { 4 });

            var resultado = await extractor.ExtraerYGuardarCSVAsync("F-001", 4, "2024 - 2025", 12, CancellationToken.None);

            Assert.True(resultado.Exitoso);
            var csv = await File.ReadAllTextAsync(resultado.RutaArchivo!);
            Assert.Contains("'=SUM(1,1)", csv, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(outputDirectory, true);
        }
    }

    [Fact]
    public async Task ProcesarAsync_ExtraeCalificacionesAunqueElPdfYaExista()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"certificados-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        await File.WriteAllBytesAsync(Path.Combine(outputDirectory, "F-001.pdf"), "%PDF-1.7"u8.ToArray());
        try
        {
            var handler = new StubHandler(request =>
            {
                if (request.Method == HttpMethod.Post)
                {
                    return Json("""
                        { "exito": true, "respuesta": {
                          "certificadoId": 12, "folio": "F-001", "cicloEscolar": "2024 - 2025",
                          "nivelEducativoId": 4, "anioInicio": 2024, "anioFin": 2025
                        } }
                        """);
                }

                Assert.Equal("/calificaciones", request.RequestUri!.AbsolutePath);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("<a id=\"A1\">Semestre 1</a><table id=\"grid1\"><tr><th>Materia</th></tr><tr><td>Matematicas</td><td>10</td><td>2024</td><td>2025</td></tr></table>", Encoding.UTF8, "text/html")
                };
            });
            using var client = new HttpClient(handler);
            var downloader = new CertificadoDownloader(
                outputDirectory,
                new Uri("https://sev.example/validacion"),
                new Uri("https://sev.example/impresion"),
                new Uri("https://sev.example/calificaciones"),
                client,
                new HashSet<int> { 4 });

            await downloader.ProcesarAsync(
                [new RegistroEntrada { Folio = "F-001", Nivel = "Nivel", Inicio = 2024, Fin = 2025, CicloEscolarId = 8, NivelEducativoId = 4 }],
                CancellationToken.None);

            Assert.True(File.Exists(Path.Combine(outputDirectory, "calificaciones", "F-001.csv")));
        }
        finally
        {
            Directory.Delete(outputDirectory, true);
        }
    }

    private static HttpResponseMessage Json(string contenido) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(contenido, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
