using System.Globalization;

namespace CertificadosDownloader.Services;

internal static class SevRequestUrlBuilder
{
    public static Uri ConParametros(Uri endpoint, params (string Nombre, object? Valor)[] parametros)
    {
        var pares = parametros
            .Where(parametro => parametro.Valor is not null)
            .Select(parametro => $"{Uri.EscapeDataString(parametro.Nombre)}={Uri.EscapeDataString(Convert.ToString(parametro.Valor, CultureInfo.InvariantCulture) ?? string.Empty)}");

        var builder = new UriBuilder(endpoint);
        var consultaActual = builder.Query.TrimStart('?');
        var parametrosCodificados = string.Join("&", pares);
        builder.Query = string.IsNullOrEmpty(consultaActual)
            ? parametrosCodificados
            : string.IsNullOrEmpty(parametrosCodificados)
                ? consultaActual
                : $"{consultaActual}&{parametrosCodificados}";

        return builder.Uri;
    }
}
