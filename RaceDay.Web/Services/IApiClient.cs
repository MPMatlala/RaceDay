namespace RaceDay.Web.Services
{
    public interface IApiClient
    {
        Task<ApiResponse<T>> GetAsync<T>(string url);

        // for calls that return a body (POST that returns an id, PUT that returns the updated object)
        Task<ApiResponse<T>> SendAsync<T>(HttpMethod method, string url, object body = null);

        // for calls with nothing useful in the response (DELETE)
        Task<ApiResponse> ExecuteAsync(HttpMethod method, string url, object body = null);

        // multipart upload. The file goes to the API, which stores it in Azure Blob Storage.
        Task<ApiResponse<T>> UploadAsync<T>(string url, IFormFile file);
    }
}
