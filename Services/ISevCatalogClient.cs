using CertificadosDownloader.Models;

namespace CertificadosDownloader.Services;

public interface ISevCatalogClient
{
    Task<RespuestaSevCatalogo<SevNivelEducativo>> ConsultarNivelesAsync(CancellationToken cancellationToken);

    Task<RespuestaSevCatalogo<SevCicloEscolar>> ConsultarCiclosAsync(int nivelEducativoId, CancellationToken cancellationToken);
}
