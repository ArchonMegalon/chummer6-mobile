using System.Text;
using System.Text.Json;

namespace Chummer.Play.Web.BrowserState;

public sealed class FileSystemBrowserKeyValueStore : IBrowserKeyValueStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _rootDirectory;
    private readonly Func<CancellationToken, Task>? _beforeCommitAsync;

    public FileSystemBrowserKeyValueStore(string rootDirectory)
        : this(rootDirectory, null)
    {
    }

    internal FileSystemBrowserKeyValueStore(string rootDirectory, Func<CancellationToken, Task>? beforeCommitAsync)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _rootDirectory = Path.GetFullPath(rootDirectory);
        _beforeCommitAsync = beforeCommitAsync;
        Directory.CreateDirectory(_rootDirectory);
    }

    public async Task<TValue?> GetAsync<TValue>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();

        string path = GetEntryPath(key);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(path))
            {
                return default;
            }

            BrowserStateEnvelope? envelope = await ReadEnvelopeAsync(path, cancellationToken);
            if (envelope is null
                || !string.Equals(envelope.Key, key, StringComparison.Ordinal)
                || envelope.PayloadJson is null)
            {
                throw CorruptEntry(path);
            }

            return JsonSerializer.Deserialize<TValue>(envelope.PayloadJson, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw CorruptEntry(path, exception);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SetAsync<TValue>(string key, TValue value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();

        string path = GetEntryPath(key);
        Directory.CreateDirectory(_rootDirectory);
        BrowserStateEnvelope envelope = new(
            key,
            JsonSerializer.Serialize(value, JsonOptions));

        await _gate.WaitAsync(cancellationToken);
        string temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        bool ownsTemporaryFile = false;
        Exception? writeFailure = null;
        try
        {
            using FileStream writeLock = AcquireWriteLock(path);
            await using (FileStream stream = new(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 16 * 1024,
                FileOptions.Asynchronous))
            {
                ownsTemporaryFile = true;
                await JsonSerializer.SerializeAsync(stream, envelope, JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }

            if (_beforeCommitAsync is not null)
            {
                await _beforeCommitAsync(cancellationToken);
            }

            // Cancellation is safe until this commit point. Once the rename succeeds,
            // report success even if the caller cancels immediately afterwards.
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, path, overwrite: true);
            ownsTemporaryFile = false;
        }
        catch (Exception exception)
        {
            writeFailure = exception;
            throw;
        }
        finally
        {
            try
            {
                if (ownsTemporaryFile)
                {
                    File.Delete(temporaryPath);
                }
            }
            catch (Exception cleanupFailure) when (writeFailure is not null
                && cleanupFailure is IOException or UnauthorizedAccessException)
            {
                // Preserve the original failure (especially cancellation) and its
                // cleanup evidence rather than replacing it with a secondary error.
                writeFailure.Data["TemporaryFileCleanupFailure"] = cleanupFailure;
            }
            finally
            {
                _gate.Release();
            }
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();

        string path = GetEntryPath(key);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            using FileStream writeLock = AcquireWriteLock(path);
            TryDelete(path);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        cancellationToken.ThrowIfCancellationRequested();

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!Directory.Exists(_rootDirectory))
            {
                return Array.Empty<string>();
            }

            List<string> keys = [];
            foreach (string path in Directory.EnumerateFiles(_rootDirectory, "*.json", SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string fileName = Path.GetFileNameWithoutExtension(path);
                string? key = TryDecodeKey(fileName);
                if (key is not null && key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    keys.Add(key);
                }
            }

            keys.Sort(StringComparer.Ordinal);
            return keys;
        }
        finally
        {
            _gate.Release();
        }
    }

    private string GetEntryPath(string key)
        => Path.Combine(_rootDirectory, $"{EncodeKey(key)}.json");

    private static async Task<BrowserStateEnvelope?> ReadEnvelopeAsync(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = new(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read | FileShare.Delete,
            bufferSize: 16 * 1024,
            FileOptions.Asynchronous);
        return await JsonSerializer.DeserializeAsync<BrowserStateEnvelope>(stream, JsonOptions, cancellationToken);
    }

    private static string EncodeKey(string key)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(key))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static InvalidDataException CorruptEntry(string path, Exception? innerException = null)
        => new($"Stored browser state at '{path}' is malformed. The file has been retained; recover or explicitly remove it before retrying.", innerException);

    private static FileStream AcquireWriteLock(string path)
    {
        // Keep this sidecar: deleting a released lock file can split concurrent
        // writers across different inodes. All store instances use the same lock.
        return new FileStream($"{path}.lock", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
    }

    private static string? TryDecodeKey(string encodedKey)
    {
        try
        {
            string normalized = encodedKey
                .Replace('-', '+')
                .Replace('_', '/');
            int remainder = normalized.Length % 4;
            if (remainder > 0)
            {
                normalized = normalized.PadRight(normalized.Length + (4 - remainder), '=');
            }

            return Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private sealed record BrowserStateEnvelope(
        string Key,
        string PayloadJson
    );
}
