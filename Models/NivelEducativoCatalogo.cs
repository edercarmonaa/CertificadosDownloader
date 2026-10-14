namespace CertificadosDownloader.Models;

public sealed class NivelEducativoCatalogo
{
    public int NivelEducativoId { get; init; }
    public required string Descripcion { get; init; }
    public bool IsActive { get; init; }
    public DateTime LastSyncedAt { get; init; }
}
