namespace CertificadosDownloader.Services;

public static class DotEnvLoader
{
    public static void CargarArchivoLocal()
    {
        var rutas = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(AppContext.BaseDirectory, ".env")
        };

        foreach (var ruta in rutas.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!File.Exists(ruta))
            {
                continue;
            }

            CargarArchivo(ruta);
            return;
        }
    }

    private static void CargarArchivo(string ruta)
    {
        var numeroLinea = 0;
        foreach (var lineaOriginal in File.ReadLines(ruta))
        {
            numeroLinea++;
            var linea = lineaOriginal.Trim();
            if (string.IsNullOrWhiteSpace(linea) || linea.StartsWith('#'))
            {
                continue;
            }

            if (linea.StartsWith("export ", StringComparison.Ordinal))
            {
                linea = linea[7..].TrimStart();
            }

            var separador = linea.IndexOf('=');
            if (separador <= 0)
            {
                throw new InvalidOperationException($"El archivo .env contiene una asignacion invalida en la linea {numeroLinea}.");
            }

            var nombre = linea[..separador].Trim();
            var valor = QuitarComillas(linea[(separador + 1)..].Trim());
            if (string.IsNullOrWhiteSpace(nombre))
            {
                throw new InvalidOperationException($"El archivo .env contiene una variable sin nombre en la linea {numeroLinea}.");
            }

            if (Environment.GetEnvironmentVariable(nombre) is null)
            {
                Environment.SetEnvironmentVariable(nombre, valor);
            }
        }
    }

    private static string QuitarComillas(string valor)
    {
        return valor.Length >= 2 &&
               ((valor.StartsWith('"') && valor.EndsWith('"')) || (valor.StartsWith('\'') && valor.EndsWith('\'')))
            ? valor[1..^1]
            : valor;
    }
}
