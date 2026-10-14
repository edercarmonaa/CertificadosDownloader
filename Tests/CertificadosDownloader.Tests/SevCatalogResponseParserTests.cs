using CertificadosDownloader.Services;
using Xunit;

namespace CertificadosDownloader.Tests;

public sealed class SevCatalogResponseParserTests
{
    [Fact]
    public void LeerNiveles_AceptaArregloDirecto_CamposAlternosYDuplicados()
    {
        var resultado = SevCatalogResponseParser.LeerNiveles("""
            [
              { "Id": "2", "Nombre": "  Media   Superior " },
              { "nivelEducativoId": 2, "descripcion": "Media Superior actualizada" },
              { "id": 0, "nombre": "Invalido" }
            ]
            """);

        var nivel = Assert.Single(resultado.Registros);
        Assert.True(resultado.EsValida);
        Assert.Equal(2, nivel.NivelEducativoId);
        Assert.Equal("Media Superior actualizada", nivel.Descripcion);
        Assert.NotEmpty(resultado.Advertencias);
    }

    [Fact]
    public void LeerCiclos_AceptaRespuestaAnidadaYExtraeAniosDeDescripcion()
    {
        var resultado = SevCatalogResponseParser.LeerCiclos("""
            { "DATA": { "items": [
              { "VALUE": "8", "descripcionCicloEscolar": " 1997 - 1997 " }
            ] } }
            """);

        var ciclo = Assert.Single(resultado.Registros);
        Assert.True(resultado.EsValida);
        Assert.Equal(8, ciclo.CicloEscolarId);
        Assert.Equal("1997 - 1997", ciclo.Descripcion);
        Assert.Equal(1997, ciclo.AnioInicio);
        Assert.Equal(1997, ciclo.AnioFin);
    }

    [Fact]
    public void LeerCiclos_ConservaUltimoDuplicado()
    {
        var resultado = SevCatalogResponseParser.LeerCiclos("""
            { "respuesta": [
              { "cicloEscolarId": 3, "descripcion": "2023 - 2024" },
              { "cicloEscolarId": 3, "descripcion": "2024 - 2025", "inicio": "2024", "fin": 2025 }
            ] }
            """);

        var ciclo = Assert.Single(resultado.Registros);
        Assert.Equal("2024 - 2025", ciclo.Descripcion);
        Assert.Equal(2024, ciclo.AnioInicio);
        Assert.Equal(2025, ciclo.AnioFin);
    }
}
