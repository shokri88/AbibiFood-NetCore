using AbibiFood.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace AbibiFood.Tests;

public sealed class ImageUploadStoreTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "AbibiUploadTests_" + Guid.NewGuid().ToString("N"));
    private ImageUploadStore Store() => new(new EnvironmentStub { WebRootPath = root }, new Messages(new ConfigurationBuilder().Build()));
    private static FormFile File(byte[] bytes, string mime, string name = "photo.png") => new(new MemoryStream(bytes), 0, bytes.Length, "imageFile", name) { Headers = new HeaderDictionary(), ContentType = mime };

    [Fact]
    public async Task SavesPngWithGeneratedName_AndCanCleanUpFailedDatabaseWrite()
    {
        var bytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aD1sAAAAASUVORK5CYII=");
        var store = Store();
        var path = await store.SaveAsync(File(bytes, "image/png", "../../attack.html"), default);
        Assert.Matches("^/uploads/catalog/[a-f0-9]{32}\\.png$", path);
        var target = Path.Combine(root, path.TrimStart('/'));
        Assert.Equal(bytes, await System.IO.File.ReadAllBytesAsync(target));
        store.DeleteNewFile(path);
        Assert.False(System.IO.File.Exists(target));
    }

    [Theory]
    [InlineData("<svg onload='alert(1)'></svg>", "image/svg+xml")]
    [InlineData("<script>alert(1)</script>", "image/png")]
    [InlineData("", "image/jpeg")]
    public async Task RejectsEmptyOrDisguisedActiveContent(string content, string mime)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Store().SaveAsync(File(System.Text.Encoding.UTF8.GetBytes(content), mime), default));
        Assert.False(Directory.Exists(root));
    }

    [Fact]
    public async Task RejectsOversizedFileBeforeWriting()
    {
        var file = new FormFile(Stream.Null, 0, ImageUploadStore.MaxBytes + 1, "imageFile", "large.png");
        await Assert.ThrowsAsync<ArgumentException>(() => Store().SaveAsync(file, default));
        Assert.False(Directory.Exists(root));
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
    private sealed class EnvironmentStub : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "Tests";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
