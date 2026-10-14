using CertificadosDownloader.Models;

namespace CertificadosDownloader.Services;

public sealed class CatalogSynchronizationService
{
    private static readonly SemaphoreSlim SynchronizationLock = new(1, 1);
    private readonly ISevCatalogClient _client;
    private readonly CatalogRepository _repository;

    public CatalogSynchronizationService(ISevCatalogClient client, CatalogRepository repository)
    {
        _client = client;
        _repository = repository;
    }

    public async Task<ResultadoSincronizacionGeneral> SincronizarTodosAsync(string actor, CancellationToken cancellationToken)
    {
        var niveles = await SincronizarNivelesAsync(actor, cancellationToken);
        var resultado = new ResultadoSincronizacionGeneral
        {
            Niveles = niveles,
            Exitoso = niveles.Exitoso,
            ActualizacionParcial = niveles.ActualizacionParcial
        };

        if (!niveles.Exitoso)
        {
            resultado.Advertencias.Add("No se consultaron ciclos porque la sincronizacion de niveles no produjo datos validos.");
            return resultado;
        }

        var nivelesActivos = await _repository.ConsultarNivelesAsync(true, cancellationToken);
        foreach (var nivel in nivelesActivos)
        {
            var ciclos = await SincronizarCiclosAsync(nivel.NivelEducativoId, actor, cancellationToken);
            resultado.CiclosProcesados += ciclos.Procesados;
            resultado.Advertencias.AddRange(ciclos.Advertencias.Select(a => $"Nivel {nivel.NivelEducativoId}: {a}"));
            if (!ciclos.Exitoso)
            {
                resultado.Exitoso = false;
                resultado.ActualizacionParcial = true;
            }
        }

        return resultado;
    }

    public async Task<ResultadoCatalogo> SincronizarNivelesAsync(string actor, CancellationToken cancellationToken)
    {
        await SynchronizationLock.WaitAsync(cancellationToken);
        try
        {
            await _repository.EnsureSchemaAsync(cancellationToken);
            var respuesta = await _client.ConsultarNivelesAsync(cancellationToken);
            if (!respuesta.EsValida || respuesta.EstaVacia)
            {
                var resultado = SinCambios(respuesta, "No se modificaron los niveles locales.");
                await _repository.RegistrarIntentoAsync(actor, "SincronizarNiveles", "Niveles", null, "SinCambios", resultado.Advertencias, cancellationToken);
                return resultado;
            }

            var procesados = await _repository.GuardarNivelesAsync(respuesta.Registros, actor, respuesta.Advertencias, cancellationToken);
            return Exitoso(procesados, respuesta.Advertencias);
        }
        finally
        {
            SynchronizationLock.Release();
        }
    }

    public async Task<ResultadoCatalogo> SincronizarCiclosAsync(int nivelEducativoId, string actor, CancellationToken cancellationToken)
    {
        await SynchronizationLock.WaitAsync(cancellationToken);
        try
        {
            await _repository.EnsureSchemaAsync(cancellationToken);
            if (!await _repository.NivelExisteAsync(nivelEducativoId, cancellationToken))
            {
                var resultado = Advertencia($"El nivel {nivelEducativoId} no existe en el catalogo local.");
                await _repository.RegistrarIntentoAsync(actor, "SincronizarCiclos", "Ciclos", nivelEducativoId, "Fallido", resultado.Advertencias, cancellationToken);
                return resultado;
            }

            var respuesta = await _client.ConsultarCiclosAsync(nivelEducativoId, cancellationToken);
            if (!respuesta.EsValida || respuesta.EstaVacia)
            {
                var resultado = SinCambios(respuesta, $"No se modificaron los ciclos locales del nivel {nivelEducativoId}.");
                await _repository.RegistrarIntentoAsync(actor, "SincronizarCiclos", "Ciclos", nivelEducativoId, "SinCambios", resultado.Advertencias, cancellationToken);
                return resultado;
            }

            var procesados = await _repository.GuardarCiclosAsync(nivelEducativoId, respuesta.Registros, actor, respuesta.Advertencias, cancellationToken);
            return Exitoso(procesados, respuesta.Advertencias);
        }
        finally
        {
            SynchronizationLock.Release();
        }
    }

    public async Task<ResumenCatalogos> ConsultarResumenAsync(CancellationToken cancellationToken)
    {
        await _repository.EnsureSchemaAsync(cancellationToken);
        return await _repository.ConsultarResumenAsync(cancellationToken);
    }

    public async Task<List<CicloEscolarCatalogo>> ConsultarCiclosPorNivelAsync(int nivelEducativoId, CancellationToken cancellationToken)
    {
        await _repository.EnsureSchemaAsync(cancellationToken);
        return await _repository.ConsultarCiclosPorNivelAsync(nivelEducativoId, false, cancellationToken);
    }

    public async Task<bool> CambiarEstadoNivelAsync(int nivelEducativoId, bool activo, string actor, CancellationToken cancellationToken)
    {
        await _repository.EnsureSchemaAsync(cancellationToken);
        return await _repository.CambiarEstadoNivelAsync(nivelEducativoId, activo, actor, cancellationToken);
    }

    public async Task<bool> CambiarEstadoCicloAsync(int nivelEducativoId, int cicloEscolarId, bool activo, string actor, CancellationToken cancellationToken)
    {
        await _repository.EnsureSchemaAsync(cancellationToken);
        return await _repository.CambiarEstadoCicloAsync(nivelEducativoId, cicloEscolarId, activo, actor, cancellationToken);
    }

    private static ResultadoCatalogo Exitoso(int procesados, IEnumerable<string> advertencias)
    {
        var resultado = new ResultadoCatalogo { Exitoso = true, Procesados = procesados };
        resultado.Advertencias.AddRange(advertencias);
        return resultado;
    }

    private static ResultadoCatalogo SinCambios<T>(RespuestaSevCatalogo<T> respuesta, string mensaje)
    {
        var resultado = new ResultadoCatalogo { Exitoso = false, ActualizacionParcial = true };
        resultado.Advertencias.AddRange(respuesta.Advertencias);
        resultado.Advertencias.Add(mensaje);
        return resultado;
    }

    private static ResultadoCatalogo Advertencia(string mensaje)
    {
        var resultado = new ResultadoCatalogo { Exitoso = false };
        resultado.Advertencias.Add(mensaje);
        return resultado;
    }
}
