using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests
{
    public class AuthTests : ApiTestBase
    {
        public AuthTests(RaceDayFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task RegisterOrganiser_WithValidDetails_Returns201()
        {
            var client = Factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/register/organiser", new
            {
                fullName = "Sipho Organiser",
                email = "sipho.org@test.com",
                password = "Password1",
                phoneNumber = "0821112222"
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task RegisterParticipant_WithValidDetails_Returns201()
        {
            var client = Factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/register/participant", new
            {
                fullName = "Thandi Participant",
                email = "thandi.part@test.com",
                password = "Password1",
                phoneNumber = "0823334444"
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Register_WithSameEmailTwice_Returns409()
        {
            var client = Factory.CreateClient();
            var details = new
            {
                fullName = "Duplicate User",
                email = "duplicate@test.com",
                password = "Password1",
                phoneNumber = "0825556666"
            };

            await client.PostAsJsonAsync("/api/auth/register/participant", details);
            var second = await client.PostAsJsonAsync("/api/auth/register/participant", details);

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task Register_WithInvalidEmail_Returns400()
        {
            var client = Factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/register/participant", new
            {
                fullName = "Bad Email",
                email = "not-an-email",
                password = "Password1",
                phoneNumber = "0821234567"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithCorrectDetails_ReturnsTokenAndRole()
        {
            var client = Factory.CreateClient();
            await client.PostAsJsonAsync("/api/auth/register/organiser", new
            {
                fullName = "Login Organiser",
                email = "login.org@test.com",
                password = "Password1",
                phoneNumber = "0821234567"
            });

            var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "login.org@test.com", password = "Password1" });
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(string.IsNullOrEmpty(json.GetProperty("token").GetString()));
            Assert.Equal("Organiser", json.GetProperty("role").GetString());
        }

        [Fact]
        public async Task Login_WithWrongPassword_Returns401()
        {
            var client = Factory.CreateClient();
            await client.PostAsJsonAsync("/api/auth/register/participant", new
            {
                fullName = "Wrong Password",
                email = "wrongpw@test.com",
                password = "Password1",
                phoneNumber = "0821234567"
            });

            var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "wrongpw@test.com", password = "WrongPassword" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithUnknownEmail_Returns401()
        {
            var client = Factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "nobody@test.com", password = "Password1" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
