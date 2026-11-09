using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests
{
    public class EventTests : ApiTestBase
    {
        public EventTests(RaceDayFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task CreateEvent_AsOrganiser_Returns201()
        {
            var organiser = await CreateClientAs("Organiser");

            var response = await organiser.PostAsJsonAsync("/api/events", new
            {
                eventName = "Soweto Marathon",
                eventDate = DateTime.Today.AddDays(60),
                location = "Soweto, Gauteng",
                description = "Annual marathon",
                distance = 42.2,
                eventType = "Run"
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_WithoutEventName_Returns400()
        {
            var organiser = await CreateClientAs("Organiser");

            var response = await organiser.PostAsJsonAsync("/api/events", new
            {
                eventDate = DateTime.Today.AddDays(60),
                location = "Soweto, Gauteng",
                description = "No name",
                distance = 10,
                eventType = "Run"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_WithInvalidEventType_Returns400()
        {
            var organiser = await CreateClientAs("Organiser");

            var response = await organiser.PostAsJsonAsync("/api/events", new
            {
                eventName = "Swim Event",
                eventDate = DateTime.Today.AddDays(60),
                location = "Durban",
                description = "Not allowed",
                distance = 5,
                eventType = "Swim"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_WithoutLoggingIn_Returns401()
        {
            var client = Factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/events", new
            {
                eventName = "Sneaky Event",
                eventDate = DateTime.Today.AddDays(60),
                location = "Cape Town",
                description = "No token",
                distance = 10,
                eventType = "Run"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UpdateEvent_AsOwner_Returns200()
        {
            var organiser = await CreateClientAs("Organiser");
            int eventId = await CreateEvent(organiser);

            var response = await organiser.PutAsJsonAsync("/api/events/" + eventId, new
            {
                eventName = "Updated Name",
                eventDate = DateTime.Today.AddDays(40),
                location = "Pretoria",
                description = "Updated",
                distance = 15,
                eventType = "Walk"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateEvent_AsDifferentOrganiser_Returns403()
        {
            var owner = await CreateClientAs("Organiser");
            var otherOrganiser = await CreateClientAs("Organiser");
            int eventId = await CreateEvent(owner);

            var response = await otherOrganiser.PutAsJsonAsync("/api/events/" + eventId, new
            {
                eventName = "Stolen",
                eventDate = DateTime.Today.AddDays(40),
                location = "Pretoria",
                description = "Not mine",
                distance = 15,
                eventType = "Run"
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateEvent_ThatDoesNotExist_Returns404()
        {
            var organiser = await CreateClientAs("Organiser");

            var response = await organiser.PutAsJsonAsync("/api/events/99999", new
            {
                eventName = "Ghost",
                eventDate = DateTime.Today.AddDays(40),
                location = "Nowhere",
                description = "Missing",
                distance = 15,
                eventType = "Run"
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteEvent_AsOwner_Returns204()
        {
            var organiser = await CreateClientAs("Organiser");
            int eventId = await CreateEvent(organiser);

            var response = await organiser.DeleteAsync("/api/events/" + eventId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteEvent_AsDifferentOrganiser_Returns403()
        {
            var owner = await CreateClientAs("Organiser");
            var otherOrganiser = await CreateClientAs("Organiser");
            int eventId = await CreateEvent(owner);

            var response = await otherOrganiser.DeleteAsync("/api/events/" + eventId);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteEvent_WithEnrolments_Returns409()
        {
            var organiser = await CreateClientAs("Organiser");
            var participant = await CreateClientAs("Participant");
            int eventId = await CreateEvent(organiser);
            int categoryId = await CreateCategory(organiser, eventId);
            await participant.PostAsync("/api/categories/" + categoryId + "/enrolments", null);

            var response = await organiser.DeleteAsync("/api/events/" + eventId);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task GetEvents_WithoutLoggingIn_Returns200()
        {
            var organiser = await CreateClientAs("Organiser");
            await CreateEvent(organiser);
            var anonymous = Factory.CreateClient();

            var response = await anonymous.GetAsync("/api/events");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetEvent_AsParticipant_Returns200()
        {
            var organiser = await CreateClientAs("Organiser");
            var participant = await CreateClientAs("Participant");
            int eventId = await CreateEvent(organiser);

            var response = await participant.GetAsync("/api/events/" + eventId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetEvent_ThatDoesNotExist_Returns404()
        {
            var client = Factory.CreateClient();

            var response = await client.GetAsync("/api/events/99999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
