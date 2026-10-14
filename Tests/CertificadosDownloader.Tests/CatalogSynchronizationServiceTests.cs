using CertificadosDownloader.Models;
using CertificadosDownloader.Services;
using Xunit;

namespace CertificadosDownloader.Tests;

public sealed class CatalogSynchronizationServiceTests
{
    [Fact]
    public async Task SincronizarTodosAsync_IndicaResultadoParcialCuandoFallaUnNivelDeCiclos()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"certificados-{Guid.NewGuid():N}.db");
        try
        {
            var repository = new CatalogRepository($"Data Source={databasePath};Foreign Keys=True");
            var service = new CatalogSynchronizationService(new ClientConCiclosFallidos(), repository);

            var resultado = await service.SincronizarTodosAsync("prueba", CancellationToken.None);

            Assert.False(resultado.Exitoso);
            Assert.True(resultado.ActualizacionParcial);
            Assert.Contains(resultado.Advertencias, advertencia => advertencia.Contains("Nivel 4", StringComparison.Ordinal));
        }
        finally
        {
            TestDatabaseCleanup.Delete(databasePath);
        }
    }

    private sealed class ClientConCiclosFallidos : ISevCatalogClient
    {
        public Task<RespuestaSevCatalogo<SevNivelEducativo>> ConsultarNivelesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new RespuestaSevCatalogo<SevNivelEducativo>
            {
                EsValida = true,
                Registros = [new SevNivelEducativo(4, "Media Superior")]
            });

        public Task<RespuestaSevCatalogo<SevCicloEscolar>> ConsultarCiclosAsync(int nivelEducativoId, CancellationToken cancellationToken)
        {
            var resultado = new RespuestaSevCatalogo<SevCicloEscolar> { EsValida = false, Registros = [] };
            resultado.Advertencias.Add("SEV no disponible.");
            return Task.FromResult(resultado);
        }
    }
}
