using HealthCard.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace HealthCard.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;

    public FileStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string userId)
    {
        // Создаем папку uploads/{UserId}/
        var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", userId);

        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        // Генерируем уникальное имя файла
        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(stream);
        }

        // Возвращаем относительный путь для хранения в БД
        return Path.Combine("uploads", userId, uniqueFileName);
    }

    public Task DeleteFileAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return Task.CompletedTask;

        var fullPath = Path.Combine(_env.WebRootPath, filePath);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    public Task<byte[]> GetFileBytesAsync(string filePath)
    {
        var fullPath = Path.Combine(_env.WebRootPath, filePath);
        var bytes = File.ReadAllBytes(fullPath);
        return Task.FromResult(bytes);
    }
}