namespace AbibiFood.Api.Services;

// فایل فقط با نام تولیدشده توسط سرور نوشته می‌شود؛ نام و مسیر ارسالی کاربر قابل اعتماد نیست.
public sealed class ImageUploadStore(IWebHostEnvironment environment, Messages messages)
{
    public const long MaxBytes = 5 * 1024 * 1024;
    public const long MaxRequestBytes = MaxBytes + 64 * 1024;

    public async Task<string> SaveAsync(IFormFile file, CancellationToken ct)
    {
        if (file.Length is <= 0 or > MaxBytes) throw new ArgumentException(messages["ImageSize"]);
        await using var source = file.OpenReadStream();
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int count;
        while ((count = await source.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + count > MaxBytes) throw new ArgumentException(messages["ImageSize"]);
            await buffer.WriteAsync(chunk.AsMemory(0, count), ct);
        }
        var bytes = buffer.ToArray();
        var extension = DetectExtension(bytes);
        if (extension is null) throw new ArgumentException(messages["ImageFormat"]);
        var expectedMime = extension switch { ".jpg" => "image/jpeg", ".png" => "image/png", _ => "image/webp" };
        if (!string.Equals(file.ContentType, expectedMime, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException(messages["ImageFormat"]);
        var name = Guid.NewGuid().ToString("N") + extension;
        var folder = Path.Combine(environment.WebRootPath, "uploads", "catalog");
        Directory.CreateDirectory(folder);
        var target = Path.Combine(folder, name);
        try { await File.WriteAllBytesAsync(target, bytes, ct); }
        catch { if (File.Exists(target)) File.Delete(target); throw; }
        return "/uploads/catalog/" + name;
    }

    // بررسی امضای باینری، فایل‌های SVG/HTML و پسوندهای اجرایی را نمی‌پذیرد؛ پردازش یا تبدیل تصویر انجام نمی‌شود.
    private static string? DetectExtension(byte[] bytes)
    {
        var data = bytes.AsSpan();
        if (data.Length >= 12 && data[..8].SequenceEqual(new byte[] {137,80,78,71,13,10,26,10})) return ".png";
        if (data.Length >= 4 && data[0] == 255 && data[1] == 216 && data[2] == 255 && data[^2] == 255 && data[^1] == 217) return ".jpg";
        if (data.Length >= 16 && data[..4].SequenceEqual("RIFF"u8) && data.Slice(8,4).SequenceEqual("WEBP"u8)) return ".webp";
        return null;
    }

    // فقط فایل همین درخواست در شکست ذخیره دیتابیس پاک می‌شود؛ تصاویر قدیمی ممکن است مرجع دیگری داشته باشند.
    public void DeleteNewFile(string path)
    {
        var name = Path.GetFileName(path);
        if (path != "/uploads/catalog/" + name) throw new ArgumentException("مسیر فایل نامعتبر است.");
        File.Delete(Path.Combine(environment.WebRootPath, "uploads", "catalog", name));
    }
}
