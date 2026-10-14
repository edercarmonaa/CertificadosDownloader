using CertificadosDownloader.Models;

namespace CertificadosDownloader.Services;

public sealed class CatalogAdministrationConsole
{
    private readonly CatalogSynchronizationService _service;
    private readonly CatalogRepository _repository;

    public CatalogAdministrationConsole(CatalogSynchronizationService service, CatalogRepository repository)
    {
        _service = service;
        _repository = repository;
    }

    public async Task EjecutarAsync(string actor, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine();
            Console.WriteLine("Administracion de catalogos");
            Console.WriteLine("1. Ver resumen y niveles");
            Console.WriteLine("2. Ver ciclos de un nivel");
            Console.WriteLine("3. Sincronizar niveles y ciclos");
            Console.WriteLine("4. Sincronizar solo niveles");
            Console.WriteLine("5. Sincronizar ciclos de un nivel");
            Console.WriteLine("6. Activar o desactivar nivel");
            Console.WriteLine("7. Activar o desactivar ciclo");
            Console.WriteLine("0. Volver");
            Console.Write("Opcion: ");
            var opcion = Console.ReadLine();
            if (opcion is null)
            {
                return;
            }

            switch (opcion.Trim())
            {
                case "1": await MostrarNivelesAsync(cancellationToken); break;
                case "2": await MostrarCiclosAsync(cancellationToken); break;
                case "3": MostrarResultado(await _service.SincronizarTodosAsync(actor, cancellationToken)); break;
                case "4": MostrarResultado(await _service.SincronizarNivelesAsync(actor, cancellationToken)); break;
                case "5": await SincronizarCiclosAsync(actor, cancellationToken); break;
                case "6": await CambiarNivelAsync(actor, cancellationToken); break;
                case "7": await CambiarCicloAsync(actor, cancellationToken); break;
                case "0": return;
                default: Console.WriteLine("Opcion no valida."); break;
            }
        }
    }

    private async Task MostrarNivelesAsync(CancellationToken cancellationToken)
    {
        var resumen = await _service.ConsultarResumenAsync(cancellationToken);
        Console.WriteLine($"Niveles activos/inactivos: {resumen.NivelesActivos}/{resumen.NivelesInactivos}. Ciclos activos/inactivos: {resumen.CiclosActivos}/{resumen.CiclosInactivos}.");
        foreach (var nivel in await _repository.ConsultarNivelesAsync(false, cancellationToken))
        {
            Console.WriteLine($"{nivel.NivelEducativoId} | {nivel.Descripcion} | {(nivel.IsActive ? "Activo" : "Inactivo")} | {nivel.LastSyncedAt:u}");
        }
    }

    private async Task MostrarCiclosAsync(CancellationToken cancellationToken)
    {
        if (!LeerEntero("Nivel educativo: ", out var nivelId)) return;
        foreach (var ciclo in await _service.ConsultarCiclosPorNivelAsync(nivelId, cancellationToken))
        {
            Console.WriteLine($"{ciclo.CicloEscolarId} | Nivel {ciclo.NivelEducativoId} | {ciclo.Descripcion} | {ciclo.AnioInicio}-{ciclo.AnioFin} | {(ciclo.IsActive ? "Activo" : "Inactivo")} | {ciclo.LastSyncedAt:u}");
        }
    }

    private async Task SincronizarCiclosAsync(string actor, CancellationToken cancellationToken)
    {
        if (LeerEntero("Nivel educativo: ", out var nivelId))
            MostrarResultado(await _service.SincronizarCiclosAsync(nivelId, actor, cancellationToken));
    }

    private async Task CambiarNivelAsync(string actor, CancellationToken cancellationToken)
    {
        if (!LeerEntero("Nivel educativo: ", out var nivelId) || !LeerEstado(out var activo)) return;
        Console.WriteLine(await _service.CambiarEstadoNivelAsync(nivelId, activo, actor, cancellationToken) ? "Estado actualizado." : "Nivel no encontrado.");
    }

    private async Task CambiarCicloAsync(string actor, CancellationToken cancellationToken)
    {
        if (!LeerEntero("Nivel educativo: ", out var nivelId) || !LeerEntero("Ciclo escolar: ", out var cicloId) || !LeerEstado(out var activo)) return;
        Console.WriteLine(await _service.CambiarEstadoCicloAsync(nivelId, cicloId, activo, actor, cancellationToken) ? "Estado actualizado." : "Ciclo no encontrado para ese nivel.");
    }

    private static bool LeerEntero(string mensaje, out int valor)
    {
        Console.Write(mensaje);
        if (!int.TryParse(Console.ReadLine(), out valor) || valor <= 0)
        {
            Console.WriteLine("Debe indicar un entero positivo.");
            return false;
        }

        return true;
    }

    private static bool LeerEstado(out bool activo)
    {
        Console.Write("Estado (A=activo, I=inactivo): ");
        var valor = Console.ReadLine()?.Trim();
        activo = string.Equals(valor, "A", StringComparison.OrdinalIgnoreCase);
        if (!activo && !string.Equals(valor, "I", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Estado no valido.");
            return false;
        }

        return true;
    }

    private static void MostrarResultado(ResultadoCatalogo resultado)
    {
        Console.WriteLine($"Resultado: {(resultado.Exitoso ? "exitoso" : resultado.ActualizacionParcial ? "sin cambios" : "fallido")}. Procesados: {resultado.Procesados}. Fecha UTC: {resultado.FechaUtc:u}");
        foreach (var advertencia in resultado.Advertencias) Console.WriteLine($"Advertencia: {advertencia}");
    }

    private static void MostrarResultado(ResultadoSincronizacionGeneral resultado)
    {
        Console.WriteLine($"Sincronizacion general: {(resultado.Exitoso ? "exitosa" : resultado.ActualizacionParcial ? "parcial" : "fallida")}.");
        MostrarResultado(resultado.Niveles);
        Console.WriteLine($"Ciclos procesados: {resultado.CiclosProcesados}. Fecha UTC: {resultado.FechaUtc:u}");
        foreach (var advertencia in resultado.Advertencias) Console.WriteLine($"Advertencia: {advertencia}");
    }
}
