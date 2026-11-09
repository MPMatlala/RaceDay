using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests
{//start of namespace
    // the endpoints the MVC dashboard and "My Results" page depend on
    public class OrganiserDashboardTests : ApiTestBase
    {//start of class
        public OrganiserDashboardTests(RaceDayFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task MyEvents_ReturnsOnlyOwnEventsWithEnrolmentCount()
        {
            var organiser = await CreateClientAs("Organiser");
            var otherOrganiser = await CreateClientAs("Organiser");
            var participant = await CreateClientAs("Participant");

            int eventId = await CreateEvent(organiser);
            await CreateEvent(otherOrganiser);
            int categoryId = await CreateCategory(organiser, eventId);
            await participant.PostAsync("/api/categories/" + categoryId + "/enrolments", null);

            var response = await organiser.GetAsync("/api/organisers/me/events");
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, json.GetArrayLength());
            Assert.Equal(eventId, json[0].GetProperty("eventId").GetInt32());
            Assert.Equal(1, json[0].GetProperty("totalEnrolments").GetInt32());
            Assert.Equal(1, json[0].GetProperty("categoryCount").GetInt32());
        }

        [Fact]
        public async Task MyEvents_DoesNotCountCancelledEnrolments()
        {
            var organiser = await CreateClientAs("Organiser");
            var participant = await CreateClientAs("Participant");
            int eventId = await CreateEvent(organiser);
            int categoryId = await CreateCategory(organiser, eventId);

            var enrol = await participant.PostAsync("/api/categories/" + categoryId + "/enrolments", null);
            var enrolJson = await enrol.Content.ReadFromJsonAsync<JsonElement>();
            int enrolmentId = enrolJson.GetProperty("enrolmentId").GetInt32();
            await participant.DeleteAsync("/api/enrolments/" + enrolmentId);

            var json = await organiser.GetFromJsonAsync<JsonElement>("/api/organisers/me/events");

            Assert.Equal(0, json[0].GetProperty("totalEnrolments").GetInt32());
        }

        [Fact]
        public async Task MyEvents_AsParticipant_Returns403()
        {
            var participant = await CreateClientAs("Participant");

            var response = await participant.GetAsync("/api/organisers/me/events");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task MyEvents_WithoutLoggingIn_Returns401()
        {
            var client = Factory.CreateClient();

            var response = await client.GetAsync("/api/organisers/me/events");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task MyResults_IncludesTotalFinishersAndEventDate()
        {
            var organiser = await CreateClientAs("Organiser");
            var runnerOne = await CreateClientAs("Participant");
            var runnerTwo = await CreateClientAs("Participant");
            int eventId = await CreateEvent(organiser);
            int categoryId = await CreateCategory(organiser, eventId);

            int enrolmentOne = await Enrol(runnerOne, categoryId);
            int enrolmentTwo = await Enrol(runnerTwo, categoryId);

            await organiser.PostAsJsonAsync("/api/enrolments/" + enrolmentOne + "/results", new { finishTime = "00:45:10", position = 1 });
            await organiser.PostAsJsonAsync("/api/enrolments/" + enrolmentTwo + "/results", new { finishTime = "00:51:02", position = 2 });

            var json = await runnerTwo.GetFromJsonAsync<JsonElement>("/api/participants/me/results");

            Assert.Equal(1, json.GetArrayLength());
            Assert.Equal(2, json[0].GetProperty("position").GetInt32());
            Assert.Equal(2, json[0].GetProperty("totalFinishers").GetInt32());
            Assert.True(json[0].TryGetProperty("eventDate", out _));
        }

        private async Task<int> Enrol(HttpClient participant, int categoryId)
        {
            var response = await participant.PostAsync("/api/categories/" + categoryId + "/enrolments", null);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("enrolmentId").GetInt32();
        }
    }
}
