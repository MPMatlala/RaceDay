namespace RaceDay.Api.DTOs
{
    public class LoginResponseDto
    {
        public string Message { get; set; }
        public string Token { get; set; }
        public int Id { get; set; }
        public string Role { get; set; }
    }

    public class OrganiserProfileDto
    {
        public int OrganiserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class OrganiserPublicDto
    {
        public int OrganiserId { get; set; }
        public string FullName { get; set; }
    }

    public class ParticipantProfileDto
    {
        public int ParticipantId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string ProfilePictureUrl { get; set; }
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

    // used by the Organiser dashboard: an event plus its enrolment numbers
    public class EventSummaryDto : EventDto
    {
        public int CategoryCount { get; set; }
        public int TotalEnrolments { get; set; }
    }

    public class EventDetailDto : EventDto
    {
        public string OrganiserName { get; set; }
        public List<CategoryDto> Categories { get; set; }
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
        // number of finishers in the same category (only filled in by GET participants/me/results)
        public int TotalFinishers { get; set; }
        public TimeSpan? FinishTime { get; set; }
        public TimeSpan? ChipTime { get; set; }
        public int? Position { get; set; }
    }
}