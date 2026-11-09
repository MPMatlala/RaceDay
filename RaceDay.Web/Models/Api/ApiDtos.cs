namespace RaceDay.Web.Models.Api
{
    // These mirror the JSON the Part 2 API sends back. They are plain data holders:
    // no business rules live in this project.

    public class LoginResponse
    {//start of class
        public string Message { get; set; }
        public string Token { get; set; }
        public int Id { get; set; }
        public string Role { get; set; }
    }

    public class ParticipantProfile
    {
        public int ParticipantId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string ProfilePictureUrl { get; set; }
    }

    public class OrganiserProfile
    {
        public int OrganiserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class EventDto
    {
        public int EventId { get; set; }
        public int OrganiserId { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public decimal Distance { get; set; }
        public string EventType { get; set; }
        public string BannerUrl { get; set; }
    }

    public class EventSummaryDto : EventDto
    {
        public int CategoryCount { get; set; }
        public int TotalEnrolments { get; set; }
    }

    public class EventDetailDto : EventDto
    {
        public string OrganiserName { get; set; }
        public List<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    }

    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }
        public string CategoryName { get; set; }
        public decimal DistanceKm { get; set; }
        public decimal EntryFee { get; set; }
        public int MaxParticipants { get; set; }
        public RouteDto Route { get; set; }
    }

    public class RouteDto
    {
        public int RouteId { get; set; }
        public int CategoryId { get; set; }
        public string StartPoint { get; set; }
        public string EndPoint { get; set; }
        public int ElevationGainM { get; set; }
        public string MapUrl { get; set; }
    }

    public class EnrolmentDto
    {
        public int EnrolmentId { get; set; }
        public int ParticipantId { get; set; }
        public string ParticipantName { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public DateTime EnrolmentDate { get; set; }
        public string RaceNumber { get; set; }
        public string Status { get; set; }
    }

    public class ResultDto
    {
        public int ResultId { get; set; }
        public int EnrolmentId { get; set; }
        public string ParticipantName { get; set; }
        public string RaceNumber { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; }
        public string CategoryName { get; set; }
        public DateTime EventDate { get; set; }
        public int TotalFinishers { get; set; }
        public TimeSpan? FinishTime { get; set; }
        public TimeSpan? ChipTime { get; set; }
        public int? Position { get; set; }
    }

    public class CreatedResponse
    {
        public int EventId { get; set; }
        public int CategoryId { get; set; }
        public int ResultId { get; set; }
    }

    public class EnrolResponse
    {
        public int EnrolmentId { get; set; }
        public string RaceNumber { get; set; }
    }

    public class WeatherDto
    {
        public string Forecast { get; set; }
    }

    public class BannerUploadResponse
    {
        public string BannerUrl { get; set; }
    }

    public class PictureUploadResponse
    {
        public string ProfilePictureUrl { get; set; }
    }
}
