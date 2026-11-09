namespace RaceDay.Api.Helpers
{
    // checks an uploaded image before it is sent to Azure Blob Storage
    public static class ImageValidator
    {
        private const long MaxBytes = 5 * 1024 * 1024; // 5 MB
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        // returns an error message, or null when the file is fine
        public static string Validate(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return "Please choose an image file to upload.";
            }

            if (file.Length > MaxBytes)
            {
                return "The image is too large. The maximum size is 5 MB.";
            }

            string extension = Path.GetExtension(file.FileName ?? "").ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension) || file.ContentType == null || !file.ContentType.StartsWith("image/"))
            {
                return "Only JPG, PNG, GIF or WEBP images are allowed.";
            }

            return null;
        }
    }
}
