namespace CertificadosDownloader.Models;

public sealed record SevNivelEducativo(int NivelEducativoId, string Descripcion);

public sealed record SevCicloEscolar(int CicloEscolarId, string Descripcion, int? AnioInicio, int? AnioFin);

public sealed class RespuestaSevCatalogo<T>
{
    public bool EsValida { get; init; }
    public bool EstaVacia { get; init; }
    public required List<T> Registros { get; init; }
    public List<string> Advertencias { get; } = new();
}
