namespace HealthCard.Services.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string userId);
    Task DeleteFileAsync(string filePath);
    Task<byte[]> GetFileBytesAsync(string filePath);
}