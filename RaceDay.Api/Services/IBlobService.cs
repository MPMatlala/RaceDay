namespace RaceDay.Api.Services
{
    // all Azure Blob Storage work goes through this, so controllers (and tests) never touch Azure directly
    public interface IBlobService
    {
        // uploads the file into the given folder and returns its public URL
        Task<string> UploadAsync(string folder, IFormFile file);

        // removes an old image. Never throws - a leftover blob is not worth failing a request for.
        Task DeleteAsync(string url);
    }

    public class BlobStorageNotConfiguredException : Exception
    {
        public BlobStorageNotConfiguredException()
            : base("Azure Blob Storage is not configured. Set AzureBlob:ConnectionString.")
        {
        }
    }
}
