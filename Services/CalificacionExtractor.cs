using System.Text;
using HtmlAgilityPack;
using CertificadosDownloader.Models;

namespace CertificadosDownloader.Services;

public class CalificacionExtractor
{
    private readonly HttpClient _httpClient;
    private readonly string _baseRuta;
    private readonly Uri _urlCalificaciones;
    private readonly IReadOnlySet<int> _nivelesConCalificaciones;

    public CalificacionExtractor(string carpetaDestino, Uri urlCalificaciones, HttpClient httpClient, IReadOnlySet<int> nivelesConCalificaciones)
    {
        _baseRuta = Path.Combine(carpetaDestino, "calificaciones");
        _urlCalificaciones = urlCalificaciones;
        _httpClient = httpClient;
        _nivelesConCalificaciones = nivelesConCalificaciones;
        if (!Directory.Exists(_baseRuta))
        {
            Directory.CreateDirectory(_baseRuta);
        }
    }

    public async Task<ResultadoExtraccionCalificaciones> ExtraerYGuardarCSVAsync(string folio, int nivel, string cicloEscolar, int certificadoId, CancellationToken cancellationToken)
    {
        if (!_nivelesConCalificaciones.Contains(nivel))
        {
            return new ResultadoExtraccionCalificaciones { Aplica = false, Exitoso = true };
        }

        var url = SevRequestUrlBuilder.ConParametros(
            _urlCalificaciones,
            ("folio", folio),
            ("nivel", nivel),
            ("cicloEscolar", cicloEscolar),
            ("id", certificadoId));

        try
        {
            var html = await _httpClient.GetStringAsync(url, cancellationToken);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var csvPath = Path.Combine(_baseRuta, OutputFileName.ParaFolio(folio, ".csv", certificadoId));

            using var writer = new StreamWriter(csvPath, false, new UTF8Encoding(true));
            await writer.WriteLineAsync("Semestre,Materia,Calificacion,AnioInicio,AnioFin".AsMemory(), cancellationToken);

            for (var i = 1; i <= 6; i++)
            {
                var anchor = doc.GetElementbyId($"A{i}");
                if (anchor == null)
                {
                    continue;
                }

                var semestre = (HtmlEntity.DeEntitize(anchor.InnerText) ?? string.Empty).Trim();

                var grid = doc.GetElementbyId($"grid{i}");
                if (grid == null)
                {
                    continue;
                }

                var rows = grid.SelectNodes(".//tr[position()>1]");
                if (rows == null)
                {
                    continue;
                }

                foreach (var row in rows)
                {
                    var cols = row.SelectNodes(".//td");
                    if (cols == null || cols.Count < 4)
                    {
                        continue;
                    }

                    var materia = (HtmlEntity.DeEntitize(cols[0].InnerText) ?? string.Empty).Trim();
                    var calificacion = (HtmlEntity.DeEntitize(cols[1].InnerText) ?? string.Empty).Trim();
                    var anioInicio = (HtmlEntity.DeEntitize(cols[2].InnerText) ?? string.Empty).Trim();
                    var anioFin = (HtmlEntity.DeEntitize(cols[3].InnerText) ?? string.Empty).Trim();

                    var linea = string.Join(",",
                        EscapeCsv(semestre),
                        EscapeCsv(materia),
                        EscapeCsv(calificacion),
                        EscapeCsv(anioInicio),
                        EscapeCsv(anioFin));
                    await writer.WriteLineAsync(linea.AsMemory(), cancellationToken);
                }
            }

            Console.WriteLine("Calificaciones guardadas en la carpeta de salida.");
            return new ResultadoExtraccionCalificaciones { Aplica = true, Exitoso = true, RutaArchivo = csvPath };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var mensaje = SafeErrorMessages.ParaUsuario(ex);
            Console.WriteLine($"Error al obtener calificaciones: {mensaje}");
            return new ResultadoExtraccionCalificaciones { Aplica = true, Exitoso = false, Error = mensaje };
        }
    }

    private static string EscapeCsv(string value)
    {
        var inicio = value.AsSpan().TrimStart();
        if (!inicio.IsEmpty && inicio[0] is '=' or '+' or '-' or '@')
        {
            value = $"'{value}";
        }

        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
