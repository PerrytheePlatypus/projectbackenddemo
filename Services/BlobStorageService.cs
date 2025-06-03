using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EduSync.Configurations;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EduSync.Services
{
    public class BlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;

        public BlobStorageService(IOptions<AzureBlobStorageOptions> options)
        {
            _blobServiceClient = new BlobServiceClient(options.Value.ConnectionString);
            _containerName = options.Value.ContainerName;
        }

        public async Task<(bool Success, string Url, string Message)> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            try
            {
                // Get a reference to a container
                BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                
                // Create the container if it doesn't exist
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
                
                // Get a reference to a blob
                string uniqueFileName = $"{Guid.NewGuid()}-{fileName}";
                BlobClient blobClient = containerClient.GetBlobClient(uniqueFileName);
                
                // Upload the file
                await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType });
                
                // Return the URL to the blob
                return (true, blobClient.Uri.ToString(), "File uploaded successfully");
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Error uploading file: {ex.Message}");
            }
        }

        public async Task<bool> DeleteFileAsync(string blobUrl)
        {
            try
            {
                // Get a reference to a container
                BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                
                // Extract blob name from URL
                Uri uri = new Uri(blobUrl);
                string blobName = Path.GetFileName(uri.LocalPath);
                
                // Get a reference to a blob
                BlobClient blobClient = containerClient.GetBlobClient(blobName);
                
                // Delete the blob
                await blobClient.DeleteIfExistsAsync();
                
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
