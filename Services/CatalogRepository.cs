using Microsoft.Data.Sqlite;
using CertificadosDownloader.Models;

namespace CertificadosDownloader.Services;

public sealed class CatalogRepository
{
    private readonly string _connectionString;

    public CatalogRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = SchemaSql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> GuardarNivelesAsync(IReadOnlyCollection<SevNivelEducativo> niveles, string actor, IReadOnlyCollection<string> advertencias, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        var fecha = DateTime.UtcNow.ToString("O");

        foreach (var nivel in niveles)
        {
            await EjecutarAsync(connection, transaction, @"
INSERT INTO SevNivelesEducativos (NivelEducativoId, Descripcion, IsActive, LastSyncedAt)
VALUES ($id, $descripcion, 1, $fecha)
ON CONFLICT(NivelEducativoId) DO UPDATE SET
    Descripcion = excluded.Descripcion,
    IsActive = 1,
    LastSyncedAt = excluded.LastSyncedAt;",
                [("$id", nivel.NivelEducativoId), ("$descripcion", nivel.Descripcion), ("$fecha", fecha)], cancellationToken);
        }

        var existentes = await LeerIdsAsync(connection, transaction, "SELECT NivelEducativoId FROM SevNivelesEducativos;", [], cancellationToken);
        var recibidos = niveles.Select(n => n.NivelEducativoId).ToHashSet();
        foreach (var id in existentes.Where(id => !recibidos.Contains(id)))
        {
            await EjecutarAsync(connection, transaction, "UPDATE SevNivelesEducativos SET IsActive = 0 WHERE NivelEducativoId = $id;", [("$id", id)], cancellationToken);
        }

        await RegistrarAuditoriaAsync(connection, transaction, actor, "SincronizarNiveles", "Niveles", null, null, niveles.Count, "Exitoso", advertencias, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return niveles.Count;
    }

    public async Task<int> GuardarCiclosAsync(int nivelEducativoId, IReadOnlyCollection<SevCicloEscolar> ciclos, string actor, IReadOnlyCollection<string> advertencias, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        var fecha = DateTime.UtcNow.ToString("O");

        foreach (var ciclo in ciclos)
        {
            await EjecutarAsync(connection, transaction, @"
INSERT INTO SevCiclosEscolares (NivelEducativoId, CicloEscolarId, Descripcion, AnioInicio, AnioFin, IsActive, LastSyncedAt)
VALUES ($nivelId, $cicloId, $descripcion, $inicio, $fin, 1, $fecha)
ON CONFLICT(NivelEducativoId, CicloEscolarId) DO UPDATE SET
    Descripcion = excluded.Descripcion,
    AnioInicio = excluded.AnioInicio,
    AnioFin = excluded.AnioFin,
    IsActive = 1,
    LastSyncedAt = excluded.LastSyncedAt;",
                [
                    ("$nivelId", nivelEducativoId), ("$cicloId", ciclo.CicloEscolarId), ("$descripcion", ciclo.Descripcion),
                    ("$inicio", ciclo.AnioInicio), ("$fin", ciclo.AnioFin), ("$fecha", fecha)
                ], cancellationToken);
        }

        var existentes = await LeerIdsAsync(connection, transaction,
            "SELECT CicloEscolarId FROM SevCiclosEscolares WHERE NivelEducativoId = $nivelId;", [("$nivelId", nivelEducativoId)], cancellationToken);
        var recibidos = ciclos.Select(c => c.CicloEscolarId).ToHashSet();
        foreach (var id in existentes.Where(id => !recibidos.Contains(id)))
        {
            await EjecutarAsync(connection, transaction,
                "UPDATE SevCiclosEscolares SET IsActive = 0 WHERE NivelEducativoId = $nivelId AND CicloEscolarId = $cicloId;",
                [("$nivelId", nivelEducativoId), ("$cicloId", id)], cancellationToken);
        }

        await RegistrarAuditoriaAsync(connection, transaction, actor, "SincronizarCiclos", "Ciclos", nivelEducativoId, null, ciclos.Count, "Exitoso", advertencias, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ciclos.Count;
    }

    public async Task<bool> NivelExisteAsync(int nivelEducativoId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM SevNivelesEducativos WHERE NivelEducativoId = $id;";
        command.Parameters.AddWithValue("$id", nivelEducativoId);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task RegistrarIntentoAsync(string actor, string accion, string catalogo, int? nivelEducativoId, string resultado, IReadOnlyCollection<string> advertencias, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await RegistrarAuditoriaAsync(connection, transaction, actor, accion, catalogo, nivelEducativoId, null, 0, resultado, advertencias, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<List<NivelEducativoCatalogo>> ConsultarNivelesAsync(bool soloActivos, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT NivelEducativoId, Descripcion, IsActive, LastSyncedAt FROM SevNivelesEducativos {(soloActivos ? "WHERE IsActive = 1" : string.Empty)} ORDER BY Descripcion;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var niveles = new List<NivelEducativoCatalogo>();
        while (await reader.ReadAsync(cancellationToken))
        {
            niveles.Add(new NivelEducativoCatalogo
            {
                NivelEducativoId = reader.GetInt32(0),
                Descripcion = reader.GetString(1),
                IsActive = reader.GetInt32(2) == 1,
                LastSyncedAt = DateTime.Parse(reader.GetString(3), null, System.Globalization.DateTimeStyles.RoundtripKind)
            });
        }

        return niveles;
    }

    public async Task<List<CicloEscolarCatalogo>> ConsultarCiclosPorNivelAsync(int nivelEducativoId, bool soloActivos, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $@"SELECT NivelEducativoId, CicloEscolarId, Descripcion, AnioInicio, AnioFin, IsActive, LastSyncedAt
FROM SevCiclosEscolares WHERE NivelEducativoId = $id {(soloActivos ? "AND IsActive = 1" : string.Empty)}
ORDER BY AnioInicio DESC, AnioFin DESC, Descripcion;";
        command.Parameters.AddWithValue("$id", nivelEducativoId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var ciclos = new List<CicloEscolarCatalogo>();
        while (await reader.ReadAsync(cancellationToken))
        {
            ciclos.Add(new CicloEscolarCatalogo
            {
                NivelEducativoId = reader.GetInt32(0),
                CicloEscolarId = reader.GetInt32(1),
                Descripcion = reader.GetString(2),
                AnioInicio = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                AnioFin = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                IsActive = reader.GetInt32(5) == 1,
                LastSyncedAt = DateTime.Parse(reader.GetString(6), null, System.Globalization.DateTimeStyles.RoundtripKind)
            });
        }

        return ciclos;
    }

    public async Task<ResumenCatalogos> ConsultarResumenAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT
    (SELECT COUNT(*) FROM SevNivelesEducativos WHERE IsActive = 1),
    (SELECT COUNT(*) FROM SevNivelesEducativos WHERE IsActive = 0),
    (SELECT COUNT(*) FROM SevCiclosEscolares WHERE IsActive = 1),
    (SELECT COUNT(*) FROM SevCiclosEscolares WHERE IsActive = 0);";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return new ResumenCatalogos
        {
            NivelesActivos = checked((int)reader.GetInt64(0)), NivelesInactivos = checked((int)reader.GetInt64(1)),
            CiclosActivos = checked((int)reader.GetInt64(2)), CiclosInactivos = checked((int)reader.GetInt64(3))
        };
    }

    public Task<bool> CambiarEstadoNivelAsync(int nivelEducativoId, bool activo, string actor, CancellationToken cancellationToken) =>
        CambiarEstadoAsync("SevNivelesEducativos", "NivelEducativoId = $nivelId", [("$nivelId", nivelEducativoId)], activo, actor, "Niveles", nivelEducativoId, null, cancellationToken);

    public Task<bool> CambiarEstadoCicloAsync(int nivelEducativoId, int cicloEscolarId, bool activo, string actor, CancellationToken cancellationToken) =>
        CambiarEstadoAsync("SevCiclosEscolares", "NivelEducativoId = $nivelId AND CicloEscolarId = $cicloId", [("$nivelId", nivelEducativoId), ("$cicloId", cicloEscolarId)], activo, actor, "Ciclos", nivelEducativoId, cicloEscolarId, cancellationToken);

    private async Task<bool> CambiarEstadoAsync(string tabla, string condicion, IEnumerable<(string, object?)> parametros, bool activo, string actor, string catalogo, int nivelId, int? cicloId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        var valores = parametros.Append(("$activo", (object?)(activo ? 1 : 0))).ToList();
        var afectados = await EjecutarAsync(connection, transaction, $"UPDATE {tabla} SET IsActive = $activo WHERE {condicion};", valores, cancellationToken);
        if (afectados > 0)
        {
            await RegistrarAuditoriaAsync(connection, transaction, actor, activo ? "Activar" : "Desactivar", catalogo, nivelId, cicloId, 1, "Exitoso", [], cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return afectados > 0;
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 60000;";
        await command.ExecuteNonQueryAsync(cancellationToken);
        return connection;
    }

    private static async Task<List<int>> LeerIdsAsync(SqliteConnection connection, SqliteTransaction transaction, string sql, IEnumerable<(string, object?)> parametros, CancellationToken cancellationToken)
    {
        await using var command = CrearComando(connection, transaction, sql, parametros);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var ids = new List<int>();
        while (await reader.ReadAsync(cancellationToken)) ids.Add(reader.GetInt32(0));
        return ids;
    }

    private static async Task<int> EjecutarAsync(SqliteConnection connection, SqliteTransaction transaction, string sql, IEnumerable<(string, object?)> parametros, CancellationToken cancellationToken)
    {
        await using var command = CrearComando(connection, transaction, sql, parametros);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static SqliteCommand CrearComando(SqliteConnection connection, SqliteTransaction transaction, string sql, IEnumerable<(string, object?)> parametros)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (nombre, valor) in parametros) command.Parameters.AddWithValue(nombre, valor ?? DBNull.Value);
        return command;
    }

    private static Task RegistrarAuditoriaAsync(SqliteConnection connection, SqliteTransaction transaction, string actor, string accion, string catalogo, int? nivelId, int? cicloId, int procesados, string resultado, IReadOnlyCollection<string> advertencias, CancellationToken cancellationToken)
    {
        return EjecutarAsync(connection, transaction, @"INSERT INTO SevCatalogAudit (Actor, OccurredAtUtc, Action, Catalog, NivelEducativoId, CicloEscolarId, ProcessedCount, Result, Warnings)
VALUES ($actor, $fecha, $accion, $catalogo, $nivel, $ciclo, $procesados, $resultado, $advertencias);",
            [
                ("$actor", actor), ("$fecha", DateTime.UtcNow.ToString("O")), ("$accion", accion), ("$catalogo", catalogo),
                ("$nivel", nivelId), ("$ciclo", cicloId), ("$procesados", procesados), ("$resultado", resultado),
                ("$advertencias", advertencias.Count == 0 ? null : string.Join(Environment.NewLine, advertencias))
            ], cancellationToken);
    }

    private const string SchemaSql = @"
CREATE TABLE IF NOT EXISTS SevNivelesEducativos (
    NivelEducativoId INTEGER NOT NULL PRIMARY KEY,
    Descripcion TEXT NOT NULL CHECK (length(Descripcion) <= 120),
    IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0, 1)),
    LastSyncedAt TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS IX_SevNivelesEducativos_Descripcion ON SevNivelesEducativos (Descripcion);
CREATE TABLE IF NOT EXISTS SevCiclosEscolares (
    NivelEducativoId INTEGER NOT NULL,
    CicloEscolarId INTEGER NOT NULL,
    Descripcion TEXT NOT NULL CHECK (length(Descripcion) <= 80),
    AnioInicio INTEGER NULL,
    AnioFin INTEGER NULL,
    IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0, 1)),
    LastSyncedAt TEXT NOT NULL,
    PRIMARY KEY (NivelEducativoId, CicloEscolarId),
    FOREIGN KEY (NivelEducativoId) REFERENCES SevNivelesEducativos (NivelEducativoId)
);
CREATE INDEX IF NOT EXISTS IX_SevCiclosEscolares_NivelDescripcion ON SevCiclosEscolares (NivelEducativoId, Descripcion);
CREATE TABLE IF NOT EXISTS SevCatalogAudit (
    AuditId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Actor TEXT NOT NULL,
    OccurredAtUtc TEXT NOT NULL,
    Action TEXT NOT NULL,
    Catalog TEXT NOT NULL,
    NivelEducativoId INTEGER NULL,
    CicloEscolarId INTEGER NULL,
    ProcessedCount INTEGER NOT NULL,
    Result TEXT NOT NULL,
    Warnings TEXT NULL
);";
}
