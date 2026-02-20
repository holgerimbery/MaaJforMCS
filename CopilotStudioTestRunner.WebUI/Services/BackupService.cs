using Microsoft.Data.Sqlite;

namespace CopilotStudioTestRunner.WebUI.Services;

public interface IBackupService
{
    /// <summary>
    /// Flushes the WAL, copies the database, and returns a readable stream for download.
    /// The stream deletes its temp file on close.
    /// </summary>
    Task<(Stream stream, string fileName)> CreateBackupStreamAsync();

    /// <summary>
    /// Validates and restores a SQLite database from an uploaded stream.
    /// Clears all connection pools before replacing the live file.
    /// </summary>
    Task RestoreAsync(Stream uploadedDb);
}

public class BackupService : IBackupService
{
    private readonly string _dbPath;

    private static ReadOnlySpan<byte> SqliteMagic =>
        "SQLite format 3\0"u8;

    public BackupService(IConfiguration configuration)
    {
        _dbPath = Path.GetFullPath(
            configuration.GetValue<string>("Storage:SqlitePath") ?? "./data/app.db");
    }

    public async Task<(Stream stream, string fileName)> CreateBackupStreamAsync()
    {
        // Flush WAL so the main db file is fully up-to-date before copying.
        var cs = $"Data Source={_dbPath};Mode=ReadWrite;";
        await using (var conn = new SqliteConnection(cs))
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA wal_checkpoint(FULL);";
            await cmd.ExecuteNonQueryAsync();
        }

        // Copy to a temp file. FileOptions.DeleteOnClose ensures cleanup even on error.
        var tempPath = Path.Combine(Path.GetTempPath(), $"maaj-backup-{Guid.NewGuid():N}.db");
        File.Copy(_dbPath, tempPath, overwrite: true);

        var fileName = $"maaj-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.db";
        Stream stream = new FileStream(
            tempPath, FileMode.Open, FileAccess.Read, FileShare.None,
            bufferSize: 81920, FileOptions.DeleteOnClose | FileOptions.Asynchronous);

        return (stream, fileName);
    }

    public async Task RestoreAsync(Stream uploadedDb)
    {
        // Buffer the entire upload so we can validate before touching the live database.
        using var ms = new MemoryStream();
        await uploadedDb.CopyToAsync(ms);
        var data = ms.ToArray();

        // Validate the SQLite magic header ("SQLite format 3\0", first 16 bytes).
        if (data.Length < 16 || !data.AsSpan(0, 16).SequenceEqual(SqliteMagic))
            throw new InvalidDataException(
                "The uploaded file is not a valid SQLite database.");

        // Write to a sibling temp file for an atomic replace.
        var dbDir = Path.GetDirectoryName(_dbPath)!;
        var tempPath = Path.Combine(dbDir, $".restore-{Guid.NewGuid():N}.db");
        await File.WriteAllBytesAsync(tempPath, data);

        // Release all pooled connections before replacing the file.
        SqliteConnection.ClearAllPools();

        // Atomically replace the live database.
        File.Move(tempPath, _dbPath, overwrite: true);
    }
}
