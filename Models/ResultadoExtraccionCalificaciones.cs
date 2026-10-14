namespace CertificadosDownloader.Models;

public sealed class ResultadoExtraccionCalificaciones
{
    public bool Aplica { get; init; }
    public bool Exitoso { get; init; }
    public string? RutaArchivo { get; init; }
    public string? Error { get; init; }
}
