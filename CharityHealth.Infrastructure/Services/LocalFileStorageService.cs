using CharityHealth.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CharityHealth.Infrastructure.Services;

public class FileStorageSettings
{
    public string BasePath { get; set; } = "uploads";
    public string BaseUrl { get; set; } = "/uploads";
}

public class LocalFileStorageService(
    IOptions<FileStorageSettings> opts,
    ILogger<LocalFileStorageService> logger) : IFileStorageService
{
    private readonly string _basePath = opts.Value.BasePath;
    private readonly string _baseUrl = opts.Value.BaseUrl;

    public async Task<string> SaveAsync(
        Stream fileStream, string fileName, string folder, CancellationToken ct = default)
    {
        // Build safe unique filename: {guid}_{originalname}
        var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var dirPath = Path.Combine(_basePath, folder);
        Directory.CreateDirectory(dirPath);

        var fullPath = Path.Combine(dirPath, safeName);

        await using var fs = File.Create(fullPath);
        await fileStream.CopyToAsync(fs, ct);

        logger.LogInformation("File saved: {Path}", fullPath);

        // Return relative path for DB storage
        return Path.Combine(folder, safeName).Replace('\\', '/');
    }

    public async Task DeleteAsync(string filePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, filePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            logger.LogInformation("File deleted: {Path}", fullPath);
        }
        await Task.CompletedTask;
    }

    public string GetPublicUrl(string filePath)
        => $"{_baseUrl}/{filePath.Replace('\\', '/')}";
}
