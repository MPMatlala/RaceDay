using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services
{
    public class BlobService : IBlobService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<BlobService> _logger;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private BlobContainerClient _container;

        public BlobService(IConfiguration config, ILogger<BlobService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<string> UploadAsync(string folder, IFormFile file)
        {
            var container = await GetContainerAsync();

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string blobName = folder.Trim('/') + "/" + Guid.NewGuid().ToString("N") + extension;
            var blob = container.GetBlobClient(blobName);

            using (var stream = file.OpenReadStream())
            {
                await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
            }

            return blob.Uri.ToString();
        }

        public async Task DeleteAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            try
            {
                var container = await GetContainerAsync();
                string path = new Uri(url).AbsolutePath.TrimStart('/');
                string prefix = container.Name + "/";

                // the path looks like "<container>/<folder>/<file>" (or "<account>/<container>/..." on Azurite)
                int index = path.IndexOf(prefix, StringComparison.Ordinal);
                if (index < 0)
                {
                    return;
                }

                string blobName = Uri.UnescapeDataString(path.Substring(index + prefix.Length));
                await container.DeleteBlobIfExistsAsync(blobName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete old blob {Url}", url);
            }
        }

        // the container is created the first time it is needed
        private async Task<BlobContainerClient> GetContainerAsync()
        {
            if (_container != null)
            {
                return _container;
            }

            await _lock.WaitAsync();
            try
            {
                if (_container == null)
                {
                    string connectionString = _config["AzureBlob:ConnectionString"];
                    if (string.IsNullOrWhiteSpace(connectionString))
                    {
                        throw new BlobStorageNotConfiguredException();
                    }

                    string name = _config["AzureBlob:ContainerName"];
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = "raceday-images";
                    }

                    var client = new BlobContainerClient(connectionString, name);

                    try
                    {
                        // public read access for blobs, so the browser can show the images straight from Azure
                        await client.CreateIfNotExistsAsync(PublicAccessType.Blob);
                    }
                    catch (RequestFailedException)
                    {
                        // the storage account does not allow public access - create a private container instead
                        await client.CreateIfNotExistsAsync(PublicAccessType.None);
                    }

                    _container = client;
                }

                return _container;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
