using ClosedXML.Excel;
using CertificadosDownloader.Models;
using Microsoft.Data.Sqlite;

namespace CertificadosDownloader.Services;

public class ExcelProcessor
{
    private readonly string _connectionString;

    public ExcelProcessor(string connectionString)
    {
        _connectionString = connectionString;
    }

    public List<RegistroEntrada> LeerExcelYResolverIDs(string pathExcel)
    {
        var lista = new List<RegistroEntrada>();

        using var workbook = new XLWorkbook(pathExcel);
        var hoja = workbook.Worksheets.First();
        var rango = hoja.RangeUsed();
        if (rango == null)
        {
            Console.WriteLine("La hoja esta vacia o no contiene datos.");
            return lista;
        }

        foreach (var row in rango.RowsUsed().Skip(1))
        {
            var numeroFila = row.RowNumber();
            var folio = row.Cell(1).GetString().Trim();
            var nivel = row.Cell(2).GetString().Trim();
            if (string.IsNullOrWhiteSpace(folio) || string.IsNullOrWhiteSpace(nivel) ||
                !row.Cell(3).TryGetValue<int>(out var inicio) || !row.Cell(4).TryGetValue<int>(out var fin) ||
                inicio <= 0 || fin <= 0 || inicio > fin)
            {
                Console.WriteLine($"Fila {numeroFila} omitida: folio, nivel o anios invalidos.");
                continue;
            }

            var (cicloId, nivelId) = ObtenerIdsDesdeSqlite(inicio, fin, nivel);

            if (cicloId == 0)
            {
                Console.WriteLine($"Fila {numeroFila} omitida: sin coincidencia en los catalogos activos.");
                continue;
            }

            lista.Add(new RegistroEntrada
            {
                Folio = folio,
                Nivel = nivel,
                Inicio = inicio,
                Fin = fin,
                CicloEscolarId = cicloId,
                NivelEducativoId = nivelId
            });
        }

        return lista;
    }

    private (int cicloId, int nivelId) ObtenerIdsDesdeSqlite(int inicio, int fin, string nivelNombre)
    {
        const string sql = @"
SELECT ciclo.CicloEscolarId, nivel.NivelEducativoId
FROM SevNivelesEducativos nivel
INNER JOIN SevCiclosEscolares ciclo ON ciclo.NivelEducativoId = nivel.NivelEducativoId
WHERE lower(trim(nivel.Descripcion)) = lower(trim($nombre))
  AND nivel.IsActive = 1
  AND ciclo.IsActive = 1
  AND ((ciclo.AnioInicio = $inicio AND ciclo.AnioFin = $fin) OR ciclo.Descripcion = $descripcion)
LIMIT 1;";

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$nombre", nivelNombre);
        command.Parameters.AddWithValue("$descripcion", $"{inicio} - {fin}");
        command.Parameters.AddWithValue("$inicio", inicio);
        command.Parameters.AddWithValue("$fin", fin);
        using var reader = command.ExecuteReader();

        return reader.Read()
            ? (reader.GetInt32(0), reader.GetInt32(1))
            : (0, 0);
    }
}
