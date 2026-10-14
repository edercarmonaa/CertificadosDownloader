using CertificadosDownloader.Models;
using CertificadosDownloader.Services;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CertificadosDownloader.Tests;

public sealed class SecurityRegressionTests
{
    [Theory]
    [InlineData("http://sev.example/")]
    [InlineData("https://usuario:ejemplo@sev.example/")]
    [InlineData("https://sev.example/?dato=ejemplo")]
    [InlineData("https://sev.example/#fragmento")]
    public void Configuracion_RechazaTransporteInseguroYCredencialesEnUrls(string url)
    {
        Assert.Throws<ArgumentException>(() => SevEndpointOptions.Crear(new Uri(url), TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task Descarga_NoRegistraFolioNiMensajeExterno()
    {
        var output = Path.Combine(Path.GetTempPath(), $"certificados-{Guid.NewGuid():N}");
        try
        {
            using var http = new HttpClient(new ErrorHandler());
            var downloader = new CertificadoDownloader(output,
                new Uri("https://sev.example/validacion"), new Uri("https://sev.example/impresion"),
                new Uri("https://sev.example/calificaciones"), http, new HashSet<int> { 4 });
            await downloader.ProcesarAsync(
                [new RegistroEntrada { Folio = "FICTICIO\nLINEA-INYECTADA", Nivel = "Nivel", Inicio = 2024,
                    Fin = 2025, CicloEscolarId = 8, NivelEducativoId = 4 }], CancellationToken.None);

            var log = await File.ReadAllTextAsync(Path.Combine(output, "errores.log"));
            Assert.DoesNotContain("FICTICIO", log);
            Assert.DoesNotContain("LINEA-INYECTADA", log);
            Assert.DoesNotContain("detalle-privado-de-prueba", log);
            Assert.Contains("Registro 1", log);
        }
        finally
        {
            if (Directory.Exists(output)) Directory.Delete(output, true);
        }
    }

    [Fact]
    public void Sqlite_UsaMotorPosteriorALaCorreccionDeCve20256965()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT sqlite_version();";
        Assert.True(Version.Parse((string)command.ExecuteScalar()!) >= new Version(3, 50, 2));
    }

    private sealed class ErrorHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("detalle-privado-de-prueba");
    }
}
