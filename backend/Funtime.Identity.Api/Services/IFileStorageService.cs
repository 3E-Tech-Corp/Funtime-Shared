namespace Funtime.Identity.Api.Services;

public interface IFileStorageService
{
    Task<string> UploadFileAsync(IFormFile file, string containerName);
    Task DeleteFileAsync(string fileUrl);
}
