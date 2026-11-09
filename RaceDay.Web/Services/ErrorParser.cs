using System.Net;
using System.Text.Json;

namespace RaceDay.Web.Services
{
    // turns whatever the API sent back into one short, readable sentence
    public static class ErrorParser
    {
        public static string Parse(HttpStatusCode status, string body)
        {
            // a server error never shows the API's own text
            if ((int)status < 500)
            {
                string fromBody = FromBody(body);
                if (!string.IsNullOrWhiteSpace(fromBody))
                {
                    return fromBody;
                }
            }

            switch ((int)status)
            {
                case 400: return "Some of the details are not valid. Please check and try again.";
                case 401: return "Your session has expired. Please log in again.";
                case 403: return "You do not have permission to do that.";
                case 404: return "We could not find what you were looking for.";
                case 409: return "That could not be done because it conflicts with existing information.";
                case 503: return "A service we depend on is not available right now. Please try again shortly.";
                default: return "Something went wrong on our side. Please try again.";
            }
        }

        private static string FromBody(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            string text = body.Trim();

            try
            {
                if (text.StartsWith("{"))
                {
                    using (var doc = JsonDocument.Parse(text))
                    {
                        var root = doc.RootElement;

                        // validation errors: { "errors": { "Field": ["message", ...] } }
                        if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                        {
                            var messages = new List<string>();
                            foreach (var property in errors.EnumerateObject())
                            {
                                if (property.Value.ValueKind != JsonValueKind.Array)
                                {
                                    continue;
                                }

                                foreach (var item in property.Value.EnumerateArray())
                                {
                                    if (item.ValueKind == JsonValueKind.String && !LooksTechnical(item.GetString()))
                                    {
                                        messages.Add(item.GetString());
                                    }
                                }
                            }

                            return messages.Count > 0 ? string.Join(" ", messages.Distinct()) : null;
                        }

                        return null;
                    }
                }

                if (text.StartsWith("\""))
                {
                    string value = JsonSerializer.Deserialize<string>(text);
                    return LooksTechnical(value) ? null : value;
                }
            }
            catch (JsonException)
            {
                return null;
            }

            // plain text such as "This category is full."
            if (!text.StartsWith("<") && text.Length <= 300 && !LooksTechnical(text))
            {
                return text;
            }

            return null;
        }

        private static bool LooksTechnical(string message)
        {
            return string.IsNullOrWhiteSpace(message) || message.Contains("JSON") || message.Contains("$.") || message.Contains("System.");
        }
    }
}
