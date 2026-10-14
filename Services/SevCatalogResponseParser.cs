using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using CertificadosDownloader.Models;

namespace CertificadosDownloader.Services;

public static class SevCatalogResponseParser
{
    private static readonly string[] Contenedores = ["respuesta", "data", "datos", "items", "result"];
    private static readonly Regex Espacios = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex RangoAnios = new(@"^(?<inicio>\d{4})\s*-\s*(?<fin>\d{4})$", RegexOptions.Compiled);

    public static RespuestaSevCatalogo<SevNivelEducativo> LeerNiveles(string json)
    {
        return Leer(json, item =>
        {
            if (!TryGetEntero(item, ["nivelEducativoId", "id", "value"], out var id) || id is not int nivelEducativoId ||
                !TryGetDescripcion(item, out var descripcion) || descripcion.Length > 120)
            {
                return null;
            }

            return new SevNivelEducativo(nivelEducativoId, descripcion);
        }, "nivel");
    }

    public static RespuestaSevCatalogo<SevCicloEscolar> LeerCiclos(string json)
    {
        return Leer(json, item =>
        {
            if (!TryGetEntero(item, ["cicloEscolarId", "id", "value"], out var id) || id is not int cicloEscolarId ||
                !TryGetDescripcion(item, out var descripcion) || descripcion.Length > 80)
            {
                return null;
            }

            TryGetEntero(item, ["anioInicio", "inicio"], out var inicio);
            TryGetEntero(item, ["anioFin", "fin"], out var fin);

            if ((!inicio.HasValue || !fin.HasValue) && TryObtenerAnios(descripcion, out var inicioDescripcion, out var finDescripcion))
            {
                inicio ??= inicioDescripcion;
                fin ??= finDescripcion;
            }

            return new SevCicloEscolar(cicloEscolarId, descripcion, inicio, fin);
        }, "ciclo");
    }

    private static RespuestaSevCatalogo<T> Leer<T>(
        string json,
        Func<JsonElement, T?> convertidor,
        string tipo) where T : class
    {
        var resultado = new RespuestaSevCatalogo<T> { Registros = new List<T>() };

        try
        {
            using var document = JsonDocument.Parse(json);
            var arreglo = BuscarArreglo(document.RootElement);
            if (!arreglo.HasValue)
            {
                resultado.Advertencias.Add("La respuesta de SEV no contiene un arreglo de registros reconocible.");
                return resultado;
            }

            var porId = new Dictionary<int, T>();
            foreach (var item in arreglo.Value.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    resultado.Advertencias.Add($"Se descarto un {tipo} que no es un objeto JSON.");
                    continue;
                }

                var registro = convertidor(item);
                var id = ObtenerId(registro);
                if (registro == null || id <= 0)
                {
                    resultado.Advertencias.Add($"Se descarto un {tipo} sin identificador positivo o descripcion valida.");
                    continue;
                }

                porId[id] = registro;
            }

            resultado.Registros.AddRange(porId.Values);
            return new RespuestaSevCatalogo<T>
            {
                EsValida = true,
                EstaVacia = resultado.Registros.Count == 0,
                Registros = resultado.Registros
            }.ConAdvertencias(resultado.Advertencias);
        }
        catch (JsonException)
        {
            resultado.Advertencias.Add("SEV devolvio JSON malformado.");
            return resultado;
        }
    }

    private static int ObtenerId<T>(T? registro) where T : class => registro switch
    {
        SevNivelEducativo nivel => nivel.NivelEducativoId,
        SevCicloEscolar ciclo => ciclo.CicloEscolarId,
        _ => 0
    };

    private static JsonElement? BuscarArreglo(JsonElement elemento)
    {
        if (elemento.ValueKind == JsonValueKind.Array)
        {
            return elemento;
        }

        if (elemento.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var propiedad in elemento.EnumerateObject().Where(p => Contenedores.Contains(p.Name, StringComparer.OrdinalIgnoreCase)))
        {
            var encontrado = BuscarArreglo(propiedad.Value);
            if (encontrado.HasValue)
            {
                return encontrado;
            }
        }

        foreach (var propiedad in elemento.EnumerateObject())
        {
            var encontrado = BuscarArreglo(propiedad.Value);
            if (encontrado.HasValue)
            {
                return encontrado;
            }
        }

        return null;
    }

    private static bool TryGetDescripcion(JsonElement item, out string descripcion)
    {
        descripcion = string.Empty;
        if (!TryGetValue(item, ["descripcion", "nombre", "text", "descripcionCicloEscolar"], out var valor) ||
            valor.ValueKind is not (JsonValueKind.String or JsonValueKind.Number))
        {
            return false;
        }

        descripcion = Espacios.Replace(valor.ToString().Trim(), " ");
        return !string.IsNullOrWhiteSpace(descripcion);
    }

    private static bool TryGetEntero(JsonElement item, string[] propiedades, out int? valor)
    {
        valor = null;
        if (!TryGetValue(item, propiedades, out var propiedad))
        {
            return false;
        }

        if (propiedad.ValueKind == JsonValueKind.Number && propiedad.TryGetInt32(out var numero))
        {
            valor = numero;
            return true;
        }

        if (propiedad.ValueKind == JsonValueKind.String &&
            int.TryParse(propiedad.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out numero))
        {
            valor = numero;
            return true;
        }

        return false;
    }

    private static bool TryGetValue(JsonElement item, string[] nombres, out JsonElement valor)
    {
        foreach (var propiedad in item.EnumerateObject())
        {
            if (nombres.Contains(propiedad.Name, StringComparer.OrdinalIgnoreCase))
            {
                valor = propiedad.Value;
                return true;
            }
        }

        valor = default;
        return false;
    }

    private static bool TryObtenerAnios(string descripcion, out int inicio, out int fin)
    {
        inicio = 0;
        fin = 0;
        var coincidencia = RangoAnios.Match(descripcion);
        if (!coincidencia.Success)
        {
            return false;
        }

        var inicioValido = int.TryParse(coincidencia.Groups["inicio"].Value, out inicio);
        var finValido = int.TryParse(coincidencia.Groups["fin"].Value, out fin);
        return inicioValido && finValido;
    }

    private static RespuestaSevCatalogo<T> ConAdvertencias<T>(this RespuestaSevCatalogo<T> respuesta, List<string> advertencias)
    {
        respuesta.Advertencias.AddRange(advertencias);
        return respuesta;
    }
}
