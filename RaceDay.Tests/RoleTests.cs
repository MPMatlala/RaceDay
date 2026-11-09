using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests
{
    // checks that Organisers and Participants cannot use each other's endpoints
    public class RoleTests : ApiTestBase
    {
        public RoleTests(RaceDayFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task ParticipantProfile_WithoutLoggingIn_Returns401()
        {
            var client = Factory.CreateClient();

            var response = await client.GetAsync("/api/participants/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task OrganiserProfile_AsParticipant_Returns403()
        {
            var participant = await CreateClientAs("Participant");

            var response = await participant.GetAsync("/api/organisers/me");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ParticipantProfile_AsOrganiser_Returns403()
        {
            var organiser = await CreateClientAs("Organiser");

            var response = await organiser.GetAsync("/api/participants/me");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_AsParticipant_Returns403()
        {
            var participant = await CreateClientAs("Participant");

            var response = await participant.PostAsJsonAsync("/api/events", new
            {
                eventName = "Not Allowed",
                eventDate = DateTime.Today.AddDays(30),
                location = "Cape Town",
                description = "Participants cannot create events",
                distance = 10,
                eventType = "Run"
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateCategory_AsParticipant_Returns403()
        {
            var organiser = await CreateClientAs("Organiser");
            var participant = await CreateClientAs("Participant");
            int eventId = await CreateEvent(organiser);

            var response = await participant.PostAsJsonAsync("/api/events/" + eventId + "/categories", new
            {
                categoryName = "Sneaky",
                distanceKm = 5,
                entryFee = 0,
                maxParticipants = 10
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Enrol_AsOrganiser_Returns403()
        {
            var organiser = await CreateClientAs("Organiser");
            int eventId = await CreateEvent(organiser);
            int categoryId = await CreateCategory(organiser, eventId);

            var response = await organiser.PostAsync("/api/categories/" + categoryId + "/enrolments", null);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task MyEnrolments_AsOrganiser_Returns403()
        {
            var organiser = await CreateClientAs("Organiser");

            var response = await organiser.GetAsync("/api/participants/me/enrolments");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task EventEnrolments_AsParticipant_Returns403()
        {
            var organiser = await CreateClientAs("Organiser");
            var participant = await CreateClientAs("Participant");
            int eventId = await CreateEvent(organiser);

            var response = await participant.GetAsync("/api/events/" + eventId + "/enrolments");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CaptureResult_AsParticipant_Returns403()
        {
            var participant = await CreateClientAs("Participant");

            var response = await participant.PostAsJsonAsync("/api/enrolments/1/results", new
            {
                finishTime = "01:30:00",
                chipTime = "01:29:50",
                position = 1
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task MyResults_AsOrganiser_Returns403()
        {
            var organiser = await CreateClientAs("Organiser");

            var response = await organiser.GetAsync("/api/participants/me/results");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
