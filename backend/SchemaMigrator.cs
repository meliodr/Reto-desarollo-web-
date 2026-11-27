using System.Security.Cryptography;
using Npgsql;

namespace Sisplan.Api;

internal static class SchemaMigrator
{
    private const string ResourcePrefix = "Sisplan.Api.Migrations.";

    public static async Task ApplyAsync(NpgsqlDataSource source, CancellationToken cancellationToken = default)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // El bloqueo transaccional evita aplicar la misma migración desde dos procesos.
        const string initializeSql = """
            SELECT pg_advisory_xact_lock(1856372215);
            CREATE SCHEMA IF NOT EXISTS sisplan;
            CREATE TABLE IF NOT EXISTS sisplan.migracion (
                nombre text PRIMARY KEY,
                sha256 text NOT NULL,
                aplicada_en timestamptz NOT NULL DEFAULT clock_timestamp()
            );
            """;
        await using (var initialize = new NpgsqlCommand(initializeSql, connection, transaction))
            await initialize.ExecuteNonQueryAsync(cancellationToken);

        var assembly = typeof(SchemaMigrator).Assembly;
        var resources = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal) && name.EndsWith(".sql", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (resources.Length == 0)
            throw new InvalidOperationException("El ejecutable no contiene migraciones de SISPLAN.");

        foreach (var resource in resources)
        {
            var name = resource[ResourcePrefix.Length..];
            await using var stream = assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException($"No se pudo leer la migración {name}.");
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            var bytes = buffer.ToArray();
            var checksum = Convert.ToHexString(SHA256.HashData(bytes));

            await using var lookup = new NpgsqlCommand("SELECT sha256 FROM sisplan.migracion WHERE nombre = @nombre", connection, transaction);
            lookup.Parameters.AddWithValue("nombre", name);
            if (await lookup.ExecuteScalarAsync(cancellationToken) is string appliedChecksum)
            {
                if (!string.Equals(appliedChecksum, checksum, StringComparison.Ordinal))
                    throw new InvalidOperationException($"La migración aplicada {name} tiene un contenido diferente.");
                continue;
            }

            var sql = System.Text.Encoding.UTF8.GetString(bytes);
            await using (var migration = new NpgsqlCommand(sql, connection, transaction))
                await migration.ExecuteNonQueryAsync(cancellationToken);

            await using var record = new NpgsqlCommand("INSERT INTO sisplan.migracion (nombre, sha256) VALUES (@nombre, @sha256)", connection, transaction);
            record.Parameters.AddWithValue("nombre", name);
            record.Parameters.AddWithValue("sha256", checksum);
            await record.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
