using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models.ViewModels
{
    // ---------- accounts ----------

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "That does not look like an email address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Please enter your password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public string ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Please tell us who you are.")]
        public string Role { get; set; } = "Participant";

        [Required(ErrorMessage = "Please enter your full name."), MaxLength(100)]
        [Display(Name = "Full name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Please enter your email address."), MaxLength(150)]
        [EmailAddress(ErrorMessage = "That does not look like an email address.")]
        public string Email { get; set; }

        [MaxLength(20, ErrorMessage = "The phone number is too long.")]
        [Phone(ErrorMessage = "That does not look like a phone number.")]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Please choose a password.")]
        [MinLength(6, ErrorMessage = "The password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Please repeat your password.")]
        [Compare("Password", ErrorMessage = "The two passwords do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; }
    }

    // ---------- organiser ----------

    public class DashboardViewModel
    {
        public string OrganiserName { get; set; }
        public List<EventSummaryDto> Events { get; set; } = new List<EventSummaryDto>();
        public List<EventSummaryDto> UpcomingEvents { get; set; } = new List<EventSummaryDto>();
        public int TotalEvents { get; set; }
        public int UpcomingCount { get; set; }
        public int TotalEnrolments { get; set; }
    }

    public class EventFormViewModel
    {
        public int EventId { get; set; }

        [Required(ErrorMessage = "Please enter the event name."), MaxLength(150)]
        [Display(Name = "Event name")]
        public string EventName { get; set; }

        [Required(ErrorMessage = "Please enter a description."), MaxLength(4000)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Please choose the event date.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Date")]
        public DateTime? EventDate { get; set; }

        [Required(ErrorMessage = "Please enter the location."), MaxLength(150)]
        public string Location { get; set; }

        [Required(ErrorMessage = "Please enter the distance.")]
        [Range(0.1, 1000, ErrorMessage = "The distance must be between 0.1 and 1000 km.")]
        [Display(Name = "Distance (km)")]
        public decimal? Distance { get; set; }

        [Required(ErrorMessage = "Please choose the event type.")]
        [RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "The event type must be Run, Walk or Cycle.")]
        [Display(Name = "Event type")]
        public string EventType { get; set; } = "Run";

        // the banner that is already saved (shown as a preview when editing)
        public string BannerUrl { get; set; }

        // a new banner chosen on the form
        [Display(Name = "Banner image")]
        public IFormFile Banner { get; set; }

        public bool IsEdit => EventId > 0;
    }

    public class CategoryFormViewModel
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }

        [Required(ErrorMessage = "Please name the category (for example 10km, Under 20 or Senior)."), MaxLength(100)]
        [Display(Name = "Category name")]
        public string CategoryName { get; set; }

        [Required(ErrorMessage = "Please enter the distance.")]
        [Range(0.1, 1000, ErrorMessage = "The distance must be between 0.1 and 1000 km.")]
        [Display(Name = "Distance (km)")]
        public decimal? DistanceKm { get; set; }

        [Required(ErrorMessage = "Please enter the entry fee (0 for free).")]
        [Range(0, 100000, ErrorMessage = "The entry fee must be between 0 and 100000.")]
        [Display(Name = "Entry fee (R)")]
        public decimal? EntryFee { get; set; }

        [Required(ErrorMessage = "Please enter the maximum number of participants.")]
        [Range(1, 100000, ErrorMessage = "The maximum must be at least 1.")]
        [Display(Name = "Max participants")]
        public int? MaxParticipants { get; set; }
    }

    public class CategoriesPageViewModel
    {
        public EventDetailDto Event { get; set; }
        public CategoryFormViewModel Form { get; set; } = new CategoryFormViewModel();
    }

    // used by the Categories, Enrolments and Results pages when no event has been chosen yet
    public class EventPickerViewModel
    {
        public string Title { get; set; }
        public string Intro { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string ButtonText { get; set; }
        public List<EventSummaryDto> Events { get; set; } = new List<EventSummaryDto>();
    }

    public class EnrolmentsPageViewModel
    {
        public EventDetailDto Event { get; set; }
        public List<EnrolmentDto> Enrolments { get; set; } = new List<EnrolmentDto>();
        public int? CategoryId { get; set; }
        public int ConfirmedCount { get; set; }
        public int PendingCount { get; set; }
        public int CancelledCount { get; set; }
    }

    public class ResultRow
    {
        public int EnrolmentId { get; set; }
        public int? ResultId { get; set; }
        public string ParticipantName { get; set; }
        public string RaceNumber { get; set; }
        public string CategoryName { get; set; }
        public string Status { get; set; }
        public string FinishTime { get; set; }
        public string ChipTime { get; set; }
        public int? Position { get; set; }
        public bool HasResult => ResultId.HasValue;
    }

    public class ResultsPageViewModel
    {
        public EventDetailDto Event { get; set; }
        public List<ResultRow> Rows { get; set; } = new List<ResultRow>();
        public bool EventHasHappened { get; set; }
        public int CapturedCount { get; set; }
    }

    // ---------- participant ----------

    public class EventBrowseViewModel
    {
        public List<EventDto> Events { get; set; } = new List<EventDto>();
        public HashSet<int> EnrolledEventIds { get; set; } = new HashSet<int>();

        public string Keyword { get; set; }
        public string Location { get; set; }
        public DateTime? Date { get; set; }
        public string EventType { get; set; }

        public bool HasFilters =>
            !string.IsNullOrWhiteSpace(Keyword) || !string.IsNullOrWhiteSpace(Location) || Date.HasValue || !string.IsNullOrWhiteSpace(EventType);
    }

    public class EventDetailsViewModel
    {
        public EventDetailDto Event { get; set; }

        // categories this participant is already enrolled in (not cancelled)
        public HashSet<int> EnrolledCategoryIds { get; set; } = new HashSet<int>();
    }

    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Please enter your full name."), MaxLength(100)]
        [Display(Name = "Full name")]
        public string FullName { get; set; }

        [MaxLength(20, ErrorMessage = "The phone number is too long.")]
        [Phone(ErrorMessage = "That does not look like a phone number.")]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }

        // read-only
        public string Email { get; set; }
        public string ProfilePictureUrl { get; set; }
    }
}
