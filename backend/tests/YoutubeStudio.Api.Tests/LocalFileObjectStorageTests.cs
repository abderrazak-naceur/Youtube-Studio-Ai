using System.IO;
using System.Text;
using YoutubeStudio.Api.Services.Storage;

namespace YoutubeStudio.Api.Tests;

public sealed class LocalFileObjectStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ysa-storage-test-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Put_then_get_round_trips_content()
    {
        var storage = new LocalFileObjectStorage(new ObjectStorageOptions { RootPath = _root });
        var content = Encoding.UTF8.GetBytes("hello media");

        var stored = await storage.PutAsync("videos/a/render.mp4", content, "video/mp4", CancellationToken.None);
        Assert.Equal(content.LongLength, stored.SizeBytes);

        var read = await storage.GetAsync("videos/a/render.mp4", CancellationToken.None);
        Assert.Equal(content, read);
    }

    [Fact]
    public async Task Exists_reflects_stored_objects()
    {
        var storage = new LocalFileObjectStorage(new ObjectStorageOptions { RootPath = _root });
        Assert.False(await storage.ExistsAsync("nope.bin", CancellationToken.None));

        await storage.PutAsync("here.bin", [1, 2, 3], "application/octet-stream", CancellationToken.None);
        Assert.True(await storage.ExistsAsync("here.bin", CancellationToken.None));
    }

    [Fact]
    public async Task Get_returns_null_for_missing_key()
    {
        var storage = new LocalFileObjectStorage(new ObjectStorageOptions { RootPath = _root });
        Assert.Null(await storage.GetAsync("missing.bin", CancellationToken.None));
    }

    [Fact]
    public async Task Rejects_path_traversal_keys()
    {
        var storage = new LocalFileObjectStorage(new ObjectStorageOptions { RootPath = _root });
        await Assert.ThrowsAsync<ArgumentException>(() => storage.PutAsync("../../etc/passwd", [1], "x", CancellationToken.None));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
