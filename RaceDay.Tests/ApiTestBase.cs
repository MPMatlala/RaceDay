using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests
{
    // shared helper methods for all the test classes
    public class ApiTestBase : IClassFixture<RaceDayFactory>
    {
        protected readonly RaceDayFactory Factory;

        public ApiTestBase(RaceDayFactory factory)
        {
            Factory = factory;
        }

        // registers a new user, logs in and returns a client that already has the token
        protected async Task<HttpClient> CreateClientAs(string role)
        {
            var client = Factory.CreateClient();
            string email = Guid.NewGuid().ToString("N") + "@test.com";

            string url = role == "Organiser" ? "/api/auth/register/organiser" : "/api/auth/register/participant";
            var register = await client.PostAsJsonAsync(url, new
            {
                fullName = "Test User",
                email = email,
                password = "Password1",
                phoneNumber = "0821234567"
            });
            register.EnsureSuccessStatusCode();

            var login = await client.PostAsJsonAsync("/api/auth/login", new { email = email, password = "Password1" });
            var json = await login.Content.ReadFromJsonAsync<JsonElement>();
            string token = json.GetProperty("token").GetString();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        protected async Task<int> CreateEvent(HttpClient organiser)
        {
            var response = await organiser.PostAsJsonAsync("/api/events", new
            {
                eventName = "Test Marathon",
                eventDate = DateTime.Today.AddDays(30),
                location = "Johannesburg, Gauteng",
                description = "A test event",
                distance = 21.1,
                eventType = "Run"
            });
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("eventId").GetInt32();
        }

        protected async Task<int> CreateCategory(HttpClient organiser, int eventId)
        {
            var response = await organiser.PostAsJsonAsync("/api/events/" + eventId + "/categories", new
            {
                categoryName = "10km",
                distanceKm = 10,
                entryFee = 150,
                maxParticipants = 100
            });
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("categoryId").GetInt32();
        }
    }
}
