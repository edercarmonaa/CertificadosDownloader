using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace CertificadosDownloader.Services;

public static class SafeErrorMessages
{
    public static string ParaUsuario(Exception error) => error switch
    {
        OperationCanceledException => "La operacion excedio el tiempo de espera.",
        HttpRequestException { StatusCode: { } status } => $"SEV respondio HTTP {(int)status}.",
        HttpRequestException => "No fue posible conectar con SEV.",
        JsonException => "SEV devolvio una respuesta JSON no valida.",
        InvalidDataException => "La respuesta no contiene un PDF valido o los datos esperados.",
        SqliteException => "No fue posible completar la operacion en la base local.",
        UnauthorizedAccessException => "No hay permisos para acceder a los archivos locales.",
        IOException => "No fue posible leer o guardar los archivos locales.",
        _ => "No fue posible completar la operacion. Revisa la configuracion y los datos de entrada."
    };
}
