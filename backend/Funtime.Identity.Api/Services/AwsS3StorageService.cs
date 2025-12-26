using Amazon.S3;
using Amazon.S3.Transfer;

namespace Funtime.Identity.Api.Services;

public class AwsS3StorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public AwsS3StorageService(IConfiguration configuration)
    {
        var awsConfig = configuration.GetSection("AWS");
        _bucketName = awsConfig["BucketName"] ?? "funtime-identity";

        // In production, use IAM roles. For development, use credentials:
        _s3Client = new AmazonS3Client(
            awsConfig["AccessKey"],
            awsConfig["SecretKey"],
            Amazon.RegionEndpoint.GetBySystemName(awsConfig["Region"] ?? "us-east-1")
        );
    }

    public async Task<string> UploadFileAsync(IFormFile file, string containerName)
    {
        var key = $"{containerName}/{Guid.NewGuid()}-{file.FileName}";

        using var stream = file.OpenReadStream();
        var uploadRequest = new TransferUtilityUploadRequest
        {
            InputStream = stream,
            Key = key,
            BucketName = _bucketName,
            ContentType = file.ContentType,
            CannedACL = S3CannedACL.PublicRead
        };

        var transferUtility = new TransferUtility(_s3Client);
        await transferUtility.UploadAsync(uploadRequest);

        return $"https://{_bucketName}.s3.amazonaws.com/{key}";
    }

    public async Task DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return;

        // Extract key from URL: https://bucket.s3.amazonaws.com/container/filename
        var uri = new Uri(fileUrl);
        var key = uri.AbsolutePath.TrimStart('/');

        await _s3Client.DeleteObjectAsync(_bucketName, key);
    }
}
