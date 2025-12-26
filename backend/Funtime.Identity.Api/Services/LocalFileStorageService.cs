namespace Funtime.Identity.Api.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly string _baseUrl;

    public string StorageType => "local";

    public LocalFileStorageService(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _baseUrl = configuration["Storage:LocalBaseUrl"] ?? "";
    }

    public async Task<string> UploadFileAsync(IFormFile file, string containerName)
    {
        var uploadsPath = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads", containerName);
        Directory.CreateDirectory(uploadsPath);

        var fileName = $"{Guid.NewGuid()}-{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Return relative path for local storage
        return $"/uploads/{containerName}/{fileName}";
    }

    public Task DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return Task.CompletedTask;

        var filePath = Path.Combine(_environment.WebRootPath ?? "wwwroot", fileUrl.TrimStart('/'));
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    public Task<Stream?> GetFileStreamAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return Task.FromResult<Stream?>(null);

        var filePath = Path.Combine(_environment.WebRootPath ?? "wwwroot", fileUrl.TrimStart('/'));
        if (!File.Exists(filePath)) return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> FileExistsAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return Task.FromResult(false);

        var filePath = Path.Combine(_environment.WebRootPath ?? "wwwroot", fileUrl.TrimStart('/'));
        return Task.FromResult(File.Exists(filePath));
    }
}
