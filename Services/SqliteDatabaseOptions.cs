using Microsoft.Data.Sqlite;

namespace CertificadosDownloader.Services;

public static class SqliteDatabaseOptions
{
    public static string DesdeEntorno()
    {
        var databasePath = Environment.GetEnvironmentVariable("CERTIFICADOS_SQLITE_PATH")?.Trim();
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            databasePath = Path.Combine("data", "certificados.db");
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(databasePath));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            ForeignKeys = true,
            DefaultTimeout = 60
        }.ToString();
    }
}
