namespace CertificadosDownloader.Models;

public sealed class ResultadoCatalogo
{
    public bool Exitoso { get; init; }
    public bool ActualizacionParcial { get; init; }
    public int Procesados { get; init; }
    public DateTime FechaUtc { get; init; } = DateTime.UtcNow;
    public List<string> Advertencias { get; } = new();
}

public sealed class ResumenCatalogos
{
    public int NivelesActivos { get; init; }
    public int NivelesInactivos { get; init; }
    public int CiclosActivos { get; init; }
    public int CiclosInactivos { get; init; }
}

public sealed class ResultadoSincronizacionGeneral
{
    public required ResultadoCatalogo Niveles { get; init; }
    public bool Exitoso { get; set; }
    public bool ActualizacionParcial { get; set; }
    public int CiclosProcesados { get; set; }
    public DateTime FechaUtc { get; init; } = DateTime.UtcNow;
    public List<string> Advertencias { get; } = new();
}
