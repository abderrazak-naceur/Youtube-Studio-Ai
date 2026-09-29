namespace YoutubeStudio.Api.Services.Storage;

public sealed class ObjectStorageOptions
{
    public const string SectionName = "ObjectStorage";

    /// <summary>Root path for the local filesystem backend. Defaults to a temp subfolder.</summary>
    public string RootPath { get; set; } = Path.Combine(Path.GetTempPath(), "ysa-object-storage");
}

public sealed record StoredObject(string Key, long SizeBytes, string ContentType);

/// <summary>
/// Storage abstraction for raw and generated media (SYSTEM-ARCHITECTURE §5). Portable so a
/// cloud backend (S3/Spaces) can replace the local one without changing callers.
/// </summary>
public interface IObjectStorage
{
    Task<StoredObject> PutAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken);
    Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken);
}

/// <summary>
/// Local filesystem implementation. Keys are sanitized and confined to the storage root so a
/// key cannot escape it via traversal.
/// </summary>
public sealed class LocalFileObjectStorage(ObjectStorageOptions options) : IObjectStorage
{
    public async Task<StoredObject> PutAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken)
    {
        var path = ResolvePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, content, cancellationToken);
        return new StoredObject(key, content.LongLength, contentType);
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
    {
        var path = ResolvePath(key);
        return File.Exists(path) ? await File.ReadAllBytesAsync(path, cancellationToken) : null;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(File.Exists(ResolvePath(key)));

    private string ResolvePath(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Storage key is required.", nameof(key));

        var root = Path.GetFullPath(options.RootPath);
        // Normalize separators and strip any leading traversal so the key stays under root.
        var relative = key.Replace('\\', '/').TrimStart('/');
        var combined = Path.GetFullPath(Path.Combine(root, relative));

        if (!combined.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
            !combined.Equals(root, StringComparison.Ordinal))
            throw new ArgumentException("Storage key escapes the storage root.", nameof(key));

        return combined;
    }
}
