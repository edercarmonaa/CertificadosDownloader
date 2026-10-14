using Microsoft.Data.Sqlite;

namespace CertificadosDownloader.Tests;

internal static class TestDatabaseCleanup
{
    public static void Delete(string databasePath)
    {
        SqliteConnection.ClearAllPools();
        DeleteIfExists(databasePath);
        DeleteIfExists($"{databasePath}-wal");
        DeleteIfExists($"{databasePath}-shm");
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }
}
