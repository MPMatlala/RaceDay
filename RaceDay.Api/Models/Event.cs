using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    public class Event
    {
        public int EventId { get; set; }

        public int OrganiserId { get; set; }

        [Required, MaxLength(150)]
        public string EventName { get; set; }

        [Column(TypeName = "date")]
        public DateTime EventDate { get; set; }

        [Required, MaxLength(150)]
        public string Location { get; set; }

        public string Description { get; set; }

        // added because the brief says an event must have a distance and a type
        [Column(TypeName = "decimal(6,2)")]
        public decimal Distance { get; set; }

        [Required, MaxLength(20)]
        public string EventType { get; set; }   // Run, Walk or Cycle

        // public URL of the banner image in Azure Blob Storage
        [MaxLength(500)]
        public string BannerUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Organiser Organiser { get; set; }
        public List<Category> Categories { get; set; } = new List<Category>();
    }
}