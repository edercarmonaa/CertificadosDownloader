using ClosedXML.Excel;
using CertificadosDownloader.Models;
using CertificadosDownloader.Services;
using Xunit;

namespace CertificadosDownloader.Tests;

public sealed class ExcelProcessorTests
{
    [Fact]
    public async Task LeerExcelYResolverIds_OmiteFilasInvalidasYConservaLasValidas()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"certificados-{Guid.NewGuid():N}.db");
        var excelPath = Path.Combine(Path.GetTempPath(), $"certificados-{Guid.NewGuid():N}.xlsx");
        try
        {
            var repository = new CatalogRepository($"Data Source={databasePath};Foreign Keys=True");
            await repository.EnsureSchemaAsync(CancellationToken.None);
            await repository.GuardarNivelesAsync([new SevNivelEducativo(5, "Media Superior")], "prueba", [], CancellationToken.None);
            await repository.GuardarCiclosAsync(5, [new SevCicloEscolar(9, "2024 - 2025", 2024, 2025)], "prueba", [], CancellationToken.None);

            using (var workbook = new XLWorkbook())
            {
                var hoja = workbook.AddWorksheet("Datos");
                hoja.Cell(1, 1).Value = "Folio";
                hoja.Cell(1, 2).Value = "Nivel";
                hoja.Cell(1, 3).Value = "Inicio";
                hoja.Cell(1, 4).Value = "Fin";
                hoja.Cell(2, 1).Value = "invalido";
                hoja.Cell(2, 2).Value = "Media Superior";
                hoja.Cell(2, 3).Value = "sin anio";
                hoja.Cell(2, 4).Value = 2025;
                hoja.Cell(3, 1).Value = "F-001";
                hoja.Cell(3, 2).Value = " media superior ";
                hoja.Cell(3, 3).Value = 2024;
                hoja.Cell(3, 4).Value = 2025;
                workbook.SaveAs(excelPath);
            }

            var registros = new ExcelProcessor($"Data Source={databasePath};Foreign Keys=True").LeerExcelYResolverIDs(excelPath);

            var registro = Assert.Single(registros);
            Assert.Equal("F-001", registro.Folio);
            Assert.Equal(5, registro.NivelEducativoId);
            Assert.Equal(9, registro.CicloEscolarId);
        }
        finally
        {
            if (File.Exists(excelPath)) File.Delete(excelPath);
            TestDatabaseCleanup.Delete(databasePath);
        }
    }
}
