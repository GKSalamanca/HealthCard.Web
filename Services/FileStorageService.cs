using HealthCard.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
namespace HealthCard.Services;

public class FileStorageService(IWebHostEnvironment env, IConfiguration configuration) : IFileStorageService
{
    private string Root => Path.GetFullPath(configuration["Storage:Root"] ?? Path.Combine(env.ContentRootPath, "App_Data"));
    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string userId)
    {
        if (string.IsNullOrEmpty(userId) || userId.IndexOfAny(new[] { '/', '\\', ':' }) >= 0 || userId is "." or "..")
            throw new ArgumentException("Некорректный идентификатор владельца");
        var relative = $"uploads/{userId}/{Guid.NewGuid():N}{Path.GetExtension(Path.GetFileName(fileName)).ToLowerInvariant()}";
        var fullPath = Resolve(relative, Root);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var destination = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        try { await fileStream.CopyToAsync(destination); }
        catch { await destination.DisposeAsync(); File.Delete(fullPath); throw; }
        return relative;
    }
    private static string Resolve(string path, string root)
    {
        var parts = path.Replace('\\', '/').Split('/');
        if (parts.Length != 3 || parts[0] != "uploads" || parts.Any(p => string.IsNullOrWhiteSpace(p) || p is "." or ".." || p.Contains(':')))
            throw new ArgumentException("Некорректный путь вложения");
        var full = Path.GetFullPath(Path.Combine(root, Path.Combine(parts)));
        if (!full.StartsWith(Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new ArgumentException("Путь выходит за границы хранилища");
        return full;
    }
    private string Existing(string path)
    {
        var file = Resolve(path, Root);
        return File.Exists(file) ? file : Resolve(path, env.WebRootPath);
    }
    public Task DeleteFileAsync(string path)
    {
        if (!string.IsNullOrEmpty(path)) { var full = Existing(path); if (File.Exists(full)) File.Delete(full); }
        return Task.CompletedTask;
    }
    public Task<byte[]> GetFileBytesAsync(string path) => File.ReadAllBytesAsync(Existing(path));
}
