using CertificadosDownloader.Models;
using CertificadosDownloader.Services;
using Xunit;

namespace CertificadosDownloader.Tests;

public sealed class CatalogRepositoryTests
{
    [Fact]
    public async Task GuardaCiclosConElMismoIdEnNivelesDistintos()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"certificados-{Guid.NewGuid():N}.db");
        try
        {
            var repository = new CatalogRepository($"Data Source={databasePath};Foreign Keys=True");
            await repository.EnsureSchemaAsync(CancellationToken.None);
            await repository.GuardarNivelesAsync(
                [new SevNivelEducativo(1, "Nivel uno"), new SevNivelEducativo(2, "Nivel dos")],
                "prueba", [], CancellationToken.None);

            await repository.GuardarCiclosAsync(1, [new SevCicloEscolar(10, "2024 - 2025", 2024, 2025)], "prueba", [], CancellationToken.None);
            await repository.GuardarCiclosAsync(2, [new SevCicloEscolar(10, "2023 - 2024", 2023, 2024)], "prueba", [], CancellationToken.None);

            var ciclosNivelUno = await repository.ConsultarCiclosPorNivelAsync(1, false, CancellationToken.None);
            var ciclosNivelDos = await repository.ConsultarCiclosPorNivelAsync(2, false, CancellationToken.None);

            Assert.Equal("2024 - 2025", Assert.Single(ciclosNivelUno).Descripcion);
            Assert.Equal("2023 - 2024", Assert.Single(ciclosNivelDos).Descripcion);
        }
        finally
        {
            TestDatabaseCleanup.Delete(databasePath);
        }
    }
}
