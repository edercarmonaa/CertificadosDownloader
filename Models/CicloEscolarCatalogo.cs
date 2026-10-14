namespace CertificadosDownloader.Models;

public sealed class CicloEscolarCatalogo
{
    public int NivelEducativoId { get; init; }
    public int CicloEscolarId { get; init; }
    public required string Descripcion { get; init; }
    public int? AnioInicio { get; init; }
    public int? AnioFin { get; init; }
    public bool IsActive { get; init; }
    public DateTime LastSyncedAt { get; init; }
}
