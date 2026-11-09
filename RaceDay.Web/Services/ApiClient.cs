using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaceDay.Web.Services
{
    // Talks to the Part 2 API. The token saved in the session at login is sent with every request.
    public class ApiClient : IApiClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _accessor;
        private readonly ILogger<ApiClient> _logger;

        public ApiClient(HttpClient http, IHttpContextAccessor accessor, ILogger<ApiClient> logger)
        {
            _http = http;
            _accessor = accessor;
            _logger = logger;
        }

        public Task<ApiResponse<T>> GetAsync<T>(string url)
        {
            return SendCoreAsync<T>(new HttpRequestMessage(HttpMethod.Get, url), true);
        }

        public Task<ApiResponse<T>> SendAsync<T>(HttpMethod method, string url, object body = null)
        {
            return SendCoreAsync<T>(BuildRequest(method, url, body), true);
        }

        public async Task<ApiResponse> ExecuteAsync(HttpMethod method, string url, object body = null)
        {
            return await SendCoreAsync<object>(BuildRequest(method, url, body), false);
        }

        public async Task<ApiResponse<T>> UploadAsync<T>(string url, IFormFile file)
        {
            var form = new MultipartFormDataContent();
            var stream = file.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            form.Add(fileContent, "file", file.FileName);

            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };

            try
            {
                return await SendCoreAsync<T>(request, true);
            }
            finally
            {
                stream.Dispose();
            }
        }

        private HttpRequestMessage BuildRequest(HttpMethod method, string url, object body)
        {
            var request = new HttpRequestMessage(method, url);
            if (body != null)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
            }
            return request;
        }

        private async Task<ApiResponse<T>> SendCoreAsync<T>(HttpRequestMessage request, bool readBody)
        {
            string token = _accessor.HttpContext?.Session.GetString("Token");
            bool hadToken = !string.IsNullOrEmpty(token);
            if (hadToken)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            try
            {
                using (request)
                using (var response = await _http.SendAsync(request))
                {
                    string body = await response.Content.ReadAsStringAsync();

                    // a logged-in user who gets 401 has an expired token (a wrong password at login has no token yet)
                    if (response.StatusCode == HttpStatusCode.Unauthorized && hadToken)
                    {
                        throw new SessionExpiredException();
                    }

                    var result = new ApiResponse<T>
                    {
                        StatusCode = (int)response.StatusCode,
                        Success = response.IsSuccessStatusCode
                    };

                    if (result.Success)
                    {
                        if (readBody && !string.IsNullOrWhiteSpace(body))
                        {
                            result.Data = JsonSerializer.Deserialize<T>(body, JsonOptions);
                        }
                    }
                    else
                    {
                        result.Error = ErrorParser.Parse(response.StatusCode, body);

                        // the user sees a friendly message; the real reason goes to the log (Visual Studio Output window)
                        _logger.LogWarning("RaceDay API {Method} {Uri} returned {Status}: {Body}",
                            request.Method, request.RequestUri, (int)response.StatusCode,
                            body != null && body.Length > 600 ? body.Substring(0, 600) : body);
                    }

                    return result;
                }
            }
            catch (SessionExpiredException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Could not reach the RaceDay API");
                return Failed<T>("We could not reach the RaceDay service. Please try again in a moment.");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "The RaceDay API timed out");
                return Failed<T>("The RaceDay service took too long to respond. Please try again.");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "The RaceDay API sent a response we could not read");
                return Failed<T>("We received an unexpected response from the RaceDay service.");
            }
        }

        private ApiResponse<T> Failed<T>(string message)
        {
            return new ApiResponse<T> { Success = false, StatusCode = 0, Error = message };
        }
    }
}
