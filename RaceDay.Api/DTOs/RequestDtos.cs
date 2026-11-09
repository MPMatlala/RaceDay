using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs
{
    public class RegisterDto
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; }

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; }

        [Required, MinLength(6)]
        public string Password { get; set; }

        [MaxLength(20)]
        public string PhoneNumber { get; set; }
    }

    public class LoginDto
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class UpdateProfileDto
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; }

        [MaxLength(20)]
        public string PhoneNumber { get; set; }
    }

    public class EventCreateDto
    {
        [Required, MaxLength(150)]
        public string EventName { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required, MaxLength(150)]
        public string Location { get; set; }

        public string Description { get; set; }

        [Range(0.1, 1000)]
        public decimal Distance { get; set; }

        [Required]
        [RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "EventType must be Run, Walk or Cycle.")]
        public string EventType { get; set; }
    }

    public class CategoryCreateDto
    {
        [Required, MaxLength(100)]
        public string CategoryName { get; set; }

        [Range(0.1, 1000)]
        public decimal DistanceKm { get; set; }

        [Range(0, 100000)]
        public decimal EntryFee { get; set; }

        [Range(1, 100000)]
        public int MaxParticipants { get; set; }
    }

    public class ResultCreateDto
    {
        [Required]
        public TimeSpan FinishTime { get; set; }

        public TimeSpan? ChipTime { get; set; }

        [Range(1, 1000000)]
        public int Position { get; set; }
    }
}
