namespace RaceDay.Api.Services
{
    // Stores images on the API's own disk (the "uploads" folder) instead of Azure.
    // Switch between this and Azure with Storage:Provider in appsettings.json ("Local" or "Azure").
    public class LocalFileService : IBlobService
    {
        private readonly string _root;
        private readonly IHttpContextAccessor _accessor;
        private readonly IConfiguration _config;

        public LocalFileService(IWebHostEnvironment env, IHttpContextAccessor accessor, IConfiguration config)
        {
            _root = Path.Combine(env.ContentRootPath, "uploads");
            _accessor = accessor;
            _config = config;
        }

        public async Task<string> UploadAsync(string folder, IFormFile file)
        {
            string safeFolder = folder.Replace("..", "").Trim('/');
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string fileName = Guid.NewGuid().ToString("N") + extension;

            string directory = Path.Combine(_root, safeFolder.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(directory);

            using (var stream = File.Create(Path.Combine(directory, fileName)))
            {
                await file.CopyToAsync(stream);
            }

            // the address the browser will use to load the image
            string baseUrl = _config["Storage:PublicBaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                var request = _accessor.HttpContext.Request;
                baseUrl = request.Scheme + "://" + request.Host;
            }

            return baseUrl.TrimEnd('/') + "/uploads/" + safeFolder + "/" + fileName;
        }

        public Task DeleteAsync(string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    return Task.CompletedTask;
                }

                string path = new Uri(url).AbsolutePath;
                const string marker = "/uploads/";
                int index = path.IndexOf(marker, StringComparison.Ordinal);
                if (index < 0)
                {
                    return Task.CompletedTask; // not one of ours (for example an old Azure URL)
                }

                string relative = Uri.UnescapeDataString(path.Substring(index + marker.Length));
                string full = Path.GetFullPath(Path.Combine(_root, relative));

                // never delete anything outside the uploads folder
                if (full.StartsWith(Path.GetFullPath(_root)) && File.Exists(full))
                {
                    File.Delete(full);
                }
            }
            catch (Exception)
            {
                // a leftover file is not worth failing a request for
            }

            return Task.CompletedTask;
        }
    }
}