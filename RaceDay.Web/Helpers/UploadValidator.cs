namespace RaceDay.Web.Helpers
{
    // quick check before the image is sent to the API (the API checks again)
    public static class UploadValidator
    {
        private const long MaxBytes = 5 * 1024 * 1024;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        // returns an error message, or null when the file is fine
        public static string Validate(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return "Please choose an image file.";
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
