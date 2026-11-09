using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests
{
    public class EnrolmentTests : ApiTestBase
    {
        public EnrolmentTests(RaceDayFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Enrol_AsParticipant_Returns201WithRaceNumber()
        {
            var organiser = await CreateClientAs("Organiser");
            var participant = await CreateClientAs("Participant");
            int eventId = await CreateEvent(organiser);
            int categoryId = await CreateCategory(organiser, eventId);

            var response = await participant.PostAsync("/api/categories/" + categoryId + "/enrolments", null);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.StartsWith("RD-", json.GetProperty("raceNumber").GetString());
        }

        [Fact]
        public async Task Enrol_IsRecordedInMyEnrolments()
        {
            var organiser = await CreateClientAs("Organiser");
            var participant = await CreateClientAs("Participant");
            int eventId = await CreateEvent(organiser);
            int categoryId = await CreateCategory(organiser, eventId);
            await participant.PostAsync("/api/categories/" + categoryId + "/enrolments", null);

            var response = await participant.GetAsync("/api/participants/me/enrolments");
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, json.GetArrayLength());
            Assert.Equal(categoryId, json[0].GetProperty("categoryId").GetInt32());
            Assert.Equal(eventId, json[0].GetProperty("eventId").GetInt32());
        }

        [Fact]
        public async Task Enrol_TwiceInSameCategory_Returns409()
        {
            var organiser = await CreateClientAs("Organiser");
            var participant = await CreateClientAs("Participant");
            int eventId = await CreateEvent(organiser);
            int categoryId = await CreateCategory(organiser, eventId);

            await participant.PostAsync("/api/categories/" + categoryId + "/enrolments", null);
            var second = await participant.PostAsync("/api/categories/" + categoryId + "/enrolments", null);

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task Enrol_InCategoryThatDoesNotExist_Returns404()
        {
            var participant = await CreateClientAs("Participant");

            var response = await participant.PostAsync("/api/categories/99999/enrolments", null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Enrol_WithoutLoggingIn_Returns401()
        {
            var client = Factory.CreateClient();

            var response = await client.PostAsync("/api/categories/1/enrolments", null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Organiser_CanSeeEnrolmentsForOwnEvent()
        {
            var organiser = await CreateClientAs("Organiser");
            var participant = await CreateClientAs("Participant");
            int eventId = await CreateEvent(organiser);
            int categoryId = await CreateCategory(organiser, eventId);
            await participant.PostAsync("/api/categories/" + categoryId + "/enrolments", null);

            var response = await organiser.GetAsync("/api/events/" + eventId + "/enrolments");
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, json.GetArrayLength());
        }

        [Fact]
        public async Task Organiser_CannotSeeEnrolmentsForAnotherOrganisersEvent()
        {
            var owner = await CreateClientAs("Organiser");
            var otherOrganiser = await CreateClientAs("Organiser");
            int eventId = await CreateEvent(owner);

            var response = await otherOrganiser.GetAsync("/api/events/" + eventId + "/enrolments");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CancelEnrolment_AsOwner_Returns204()
        {
            var organiser = await CreateClientAs("Organiser");
            var participant = await CreateClientAs("Participant");
            int eventId = await CreateEvent(organiser);
            int categoryId = await CreateCategory(organiser, eventId);
            var enrol = await participant.PostAsync("/api/categories/" + categoryId + "/enrolments", null);
            var json = await enrol.Content.ReadFromJsonAsync<JsonElement>();
            int enrolmentId = json.GetProperty("enrolmentId").GetInt32();

            var response = await participant.DeleteAsync("/api/enrolments/" + enrolmentId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task CancelEnrolment_OfAnotherParticipant_Returns403()
        {
            var organiser = await CreateClientAs("Organiser");
            var participant = await CreateClientAs("Participant");
            var otherParticipant = await CreateClientAs("Participant");
            int eventId = await CreateEvent(organiser);
            int categoryId = await CreateCategory(organiser, eventId);
            var enrol = await participant.PostAsync("/api/categories/" + categoryId + "/enrolments", null);
            var json = await enrol.Content.ReadFromJsonAsync<JsonElement>();
            int enrolmentId = json.GetProperty("enrolmentId").GetInt32();

            var response = await otherParticipant.DeleteAsync("/api/enrolments/" + enrolmentId);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Results_OrganiserCaptures_ParticipantCanViewOwn()
        {
            var organiser = await CreateClientAs("Organiser");
            var participant = await CreateClientAs("Participant");
            int eventId = await CreateEvent(organiser);
            int categoryId = await CreateCategory(organiser, eventId);
            var enrol = await participant.PostAsync("/api/categories/" + categoryId + "/enrolments", null);
            var enrolJson = await enrol.Content.ReadFromJsonAsync<JsonElement>();
            int enrolmentId = enrolJson.GetProperty("enrolmentId").GetInt32();

            var capture = await organiser.PostAsJsonAsync("/api/enrolments/" + enrolmentId + "/results", new
            {
                finishTime = "00:48:15",
                chipTime = "00:47:58",
                position = 15
            });
            var myResults = await participant.GetAsync("/api/participants/me/results");
            var json = await myResults.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.Created, capture.StatusCode);
            Assert.Equal(1, json.GetArrayLength());
            Assert.Equal(15, json[0].GetProperty("position").GetInt32());
        }
    }
}
