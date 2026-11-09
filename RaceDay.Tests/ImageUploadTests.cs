using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests
{
    // banner and profile picture uploads (Azure Blob Storage is replaced by a fake in these tests)
    public class ImageUploadTests : ApiTestBase
    {
        public ImageUploadTests(RaceDayFactory factory) : base(factory)
        {
        }

        private MultipartFormDataContent MakeFile(string fileName, string contentType)
        {
            var file = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
            file.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            var form = new MultipartFormDataContent();
            form.Add(file, "file", fileName);
            return form;
        }

        [Fact]
        public async Task UploadBanner_AsOwner_StoresUrlOnEvent()
        {
            var organiser = await CreateClientAs("Organiser");
            int eventId = await CreateEvent(organiser);

            var response = await organiser.PostAsync("/api/events/" + eventId + "/banner", MakeFile("banner.png", "image/png"));
            var detail = await organiser.GetFromJsonAsync<JsonElement>("/api/events/" + eventId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("/events/" + eventId + "/", detail.GetProperty("bannerUrl").GetString());
        }

        [Fact]
        public async Task UploadBanner_OnAnotherOrganisersEvent_Returns403()
        {
            var owner = await CreateClientAs("Organiser");
            var other = await CreateClientAs("Organiser");
            int eventId = await CreateEvent(owner);

            var response = await other.PostAsync("/api/events/" + eventId + "/banner", MakeFile("banner.png", "image/png"));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UploadBanner_AsParticipant_Returns403()
        {
            var organiser = await CreateClientAs("Organiser");
            var participant = await CreateClientAs("Participant");
            int eventId = await CreateEvent(organiser);

            var response = await participant.PostAsync("/api/events/" + eventId + "/banner", MakeFile("banner.png", "image/png"));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UploadBanner_WithATextFile_Returns400()
        {
            var organiser = await CreateClientAs("Organiser");
            int eventId = await CreateEvent(organiser);

            var response = await organiser.PostAsync("/api/events/" + eventId + "/banner", MakeFile("notes.txt", "text/plain"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UploadBanner_ForEventThatDoesNotExist_Returns404()
        {
            var organiser = await CreateClientAs("Organiser");

            var response = await organiser.PostAsync("/api/events/99999/banner", MakeFile("banner.png", "image/png"));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UploadPicture_AsParticipant_ShowsOnProfile()
        {
            var participant = await CreateClientAs("Participant");

            var response = await participant.PostAsync("/api/participants/me/picture", MakeFile("me.jpg", "image/jpeg"));
            var profile = await participant.GetFromJsonAsync<JsonElement>("/api/participants/me");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("/profiles/", profile.GetProperty("profilePictureUrl").GetString());
        }

        [Fact]
        public async Task UploadPicture_AsOrganiser_Returns403()
        {
            var organiser = await CreateClientAs("Organiser");

            var response = await organiser.PostAsync("/api/participants/me/picture", MakeFile("me.jpg", "image/jpeg"));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UploadPicture_WithoutLoggingIn_Returns401()
        {
            var client = Factory.CreateClient();

            var response = await client.PostAsync("/api/participants/me/picture", MakeFile("me.jpg", "image/jpeg"));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
