using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    // called RaceRoute so it does not clash with the [Route] attribute
    [Table("Routes")]
    public class RaceRoute
    {
        [Key]
        public int RouteId { get; set; }

        public int CategoryId { get; set; }

        [Required, MaxLength(150)]
        public string StartPoint { get; set; }

        [Required, MaxLength(150)]
        public string EndPoint { get; set; }

        public int ElevationGainM { get; set; }

        [MaxLength(255)]
        public string MapUrl { get; set; }

        public Category Category { get; set; }
    }
}
