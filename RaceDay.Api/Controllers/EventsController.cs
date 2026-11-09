using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers
{
    /// <summary>
    /// Create, view, update and delete events.
    /// </summary>
    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IBlobService _blobs;

        public EventsController(RaceDayContext context, IHttpClientFactory httpClientFactory, IBlobService blobs)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _blobs = blobs;
        }

        /// <summary>
        /// Lists all upcoming events.
        /// </summary>
        /// <remarks>Optional query filters: date, location and keyword (searches the name and description).</remarks>
        /// <response code="200">An array of events.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEvents(DateTime? date, string location, string keyword)
        {
            var query = _context.Events.Where(e => e.EventDate >= DateTime.Today);

            if (date.HasValue)
            {
                query = query.Where(e => e.EventDate == date.Value.Date);
            }

            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(e => e.Location.Contains(location));
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(e => e.EventName.Contains(keyword) || e.Description.Contains(keyword));
            }

            var events = await query.OrderBy(e => e.EventDate).ToListAsync();

            return Ok(events.Select(Mapper.ToEventDto).ToList());
        }

        /// <summary>
        /// Gets the full details of one event, with its categories and route summary.
        /// </summary>
        /// <response code="200">The event details.</response>
        /// <response code="404">Event not found.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEvent(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Organiser)
                .Include(e => e.Categories).ThenInclude(c => c.RaceRoute)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null)
            {
                return NotFound();
            }

            var dto = new EventDetailDto
            {
                EventId = ev.EventId,
                OrganiserId = ev.OrganiserId,
                EventName = ev.EventName,
                EventDate = ev.EventDate,
                Location = ev.Location,
                Description = ev.Description,
                Distance = ev.Distance,
                EventType = ev.EventType,
                BannerUrl = ev.BannerUrl,
                OrganiserName = ev.Organiser.FullName,
                Categories = ev.Categories.Select(Mapper.ToCategoryDto).ToList()
            };

            return Ok(dto);
        }

        /// <summary>
        /// Creates a new event owned by the logged-in Organiser.
        /// </summary>
        /// <remarks>Expects eventName, eventDate, location, description, distance and eventType (Run, Walk or Cycle). The organiserId comes from the token.</remarks>
        /// <response code="201">Event created. Returns the new eventId.</response>
        /// <response code="400">Invalid details.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as a Participant.</response>
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateEvent(EventCreateDto dto)
        {
            if (dto.EventDate.Date < DateTime.Today)
            {
                return BadRequest("The event date cannot be in the past.");
            }

            var ev = new Event
            {
                OrganiserId = User.GetUserId(),
                EventName = dto.EventName,
                EventDate = dto.EventDate.Date,
                Location = dto.Location,
                Description = dto.Description,
                Distance = dto.Distance,
                EventType = dto.EventType
            };

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { eventId = ev.EventId });
        }

        /// <summary>
        /// Updates an event that the logged-in Organiser owns.
        /// </summary>
        /// <remarks>Expects eventName, eventDate, location, description, distance and eventType.</remarks>
        /// <response code="200">The updated event.</response>
        /// <response code="400">Invalid details.</response>
        /// <response code="403">The event belongs to another Organiser (or the user is a Participant).</response>
        /// <response code="404">Event not found.</response>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEvent(int id, EventCreateDto dto)
        {
            var ev = await _context.Events.FindAsync(id);

            if (ev == null)
            {
                return NotFound();
            }

            if (ev.OrganiserId != User.GetUserId())
            {
                return Forbid();
            }

            ev.EventName = dto.EventName;
            ev.EventDate = dto.EventDate.Date;
            ev.Location = dto.Location;
            ev.Description = dto.Description;
            ev.Distance = dto.Distance;
            ev.EventType = dto.EventType;
            await _context.SaveChangesAsync();

            return Ok(Mapper.ToEventDto(ev));
        }

        /// <summary>
        /// Deletes an event that the logged-in Organiser owns.
        /// </summary>
        /// <remarks>An event with enrolments cannot be deleted. Its categories and routes are deleted with it.</remarks>
        /// <response code="204">Event deleted.</response>
        /// <response code="403">The event belongs to another Organiser (or the user is a Participant).</response>
        /// <response code="404">Event not found.</response>
        /// <response code="409">The event already has enrolments.</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Categories).ThenInclude(c => c.RaceRoute)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null)
            {
                return NotFound();
            }

            if (ev.OrganiserId != User.GetUserId())
            {
                return Forbid();
            }

            bool hasEnrolments = await _context.Enrolments.AnyAsync(e => e.Category.EventId == id);
            if (hasEnrolments)
            {
                return Conflict("This event has enrolments and cannot be deleted.");
            }

            foreach (var category in ev.Categories)
            {
                if (category.RaceRoute != null)
                {
                    _context.RaceRoutes.Remove(category.RaceRoute);
                }
                _context.Categories.Remove(category);
            }

            string bannerUrl = ev.BannerUrl;

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            await _blobs.DeleteAsync(bannerUrl);

            return NoContent();
        }

        /// <summary>
        /// Uploads (or replaces) the banner image of an event the logged-in Organiser owns.
        /// </summary>
        /// <remarks>Send the image as multipart/form-data in a field called "file". JPG, PNG, GIF or WEBP, up to 5 MB. The image is stored in Azure Blob Storage.</remarks>
        /// <response code="200">Image stored. Returns the bannerUrl.</response>
        /// <response code="400">No file, wrong type or too large.</response>
        /// <response code="403">The event belongs to another Organiser (or the user is a Participant).</response>
        /// <response code="404">Event not found.</response>
        /// <response code="503">Azure Blob Storage is not available.</response>
        [HttpPost("{id:int}/banner")]
        [Authorize(Roles = "Organiser")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> UploadBanner(int id, IFormFile file)
        {
            var ev = await _context.Events.FindAsync(id);

            if (ev == null)
            {
                return NotFound();
            }

            if (ev.OrganiserId != User.GetUserId())
            {
                return Forbid();
            }

            string error = ImageValidator.Validate(file);
            if (error != null)
            {
                return BadRequest(error);
            }

            string newUrl;
            try
            {
                newUrl = await _blobs.UploadAsync("events/" + id, file);
            }
            catch (Exception)
            {
                return StatusCode(503, "Image storage is not available right now.");
            }

            string oldUrl = ev.BannerUrl;
            ev.BannerUrl = newUrl;
            await _context.SaveChangesAsync();

            await _blobs.DeleteAsync(oldUrl);

            return Ok(new { bannerUrl = newUrl });
        }

        /// <summary>
        /// Gets the weather for the event's location.
        /// </summary>
        /// <remarks>Calls the free wttr.in weather service using the first part of the event location (e.g. Johannesburg).</remarks>
        /// <response code="200">A short weather summary.</response>
        /// <response code="404">Event not found.</response>
        /// <response code="503">The weather service could not be reached.</response>
        [HttpGet("{id:int}/weather")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GetWeather(int id)
        {
            var ev = await _context.Events.FindAsync(id);

            if (ev == null)
            {
                return NotFound();
            }

            try
            {
                string city = ev.Location.Split(',')[0].Trim();
                string url = "https://wttr.in/" + Uri.EscapeDataString(city) + "?format=3";

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                string forecast = await client.GetStringAsync(url);

                return Ok(new
                {
                    eventId = ev.EventId,
                    location = ev.Location,
                    eventDate = ev.EventDate,
                    forecast = forecast.Trim()
                });
            }
            catch (Exception)
            {
                return StatusCode(503, "The weather service is not available right now.");
            }
        }

        /// <summary>
        /// Returns the route information for all the categories of an event.
        /// </summary>
        /// <response code="200">An array of routes.</response>
        /// <response code="404">Event not found.</response>
        [HttpGet("{id:int}/route")]
        [ProducesResponseType(typeof(List<RouteDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRoutes(int id)
        {
            bool eventExists = await _context.Events.AnyAsync(e => e.EventId == id);
            if (!eventExists)
            {
                return NotFound();
            }

            var routes = await _context.RaceRoutes
                .Where(r => r.Category.EventId == id)
                .ToListAsync();

            return Ok(routes.Select(Mapper.ToRouteDto).ToList());
        }

        /// <summary>
        /// Lists all the enrolments for an event the logged-in Organiser owns.
        /// </summary>
        /// <response code="200">An array of enrolments.</response>
        /// <response code="403">The event belongs to another Organiser (or the user is a Participant).</response>
        /// <response code="404">Event not found.</response>
        [HttpGet("{eventId:int}/enrolments")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {
            var ev = await _context.Events.FindAsync(eventId);

            if (ev == null)
            {
                return NotFound();
            }

            if (ev.OrganiserId != User.GetUserId())
            {
                return Forbid();
            }

            var enrolments = await _context.Enrolments
                .Include(e => e.Participant)
                .Include(e => e.Category).ThenInclude(c => c.Event)
                .Where(e => e.Category.EventId == eventId)
                .ToListAsync();

            return Ok(enrolments.Select(Mapper.ToEnrolmentDto).ToList());
        }

        /// <summary>
        /// Lists all results for an event (leaderboard), ordered by category and position.
        /// </summary>
        /// <response code="200">An array of results.</response>
        /// <response code="404">Event not found.</response>
        [HttpGet("{eventId:int}/results")]
        [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEventResults(int eventId)
        {
            bool eventExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
            if (!eventExists)
            {
                return NotFound();
            }

            var results = await _context.Results
                .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
                .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event)
                .Where(r => r.Enrolment.Category.EventId == eventId)
                .ToListAsync();

            var ordered = results
                .OrderBy(r => r.Enrolment.Category.CategoryName)
                .ThenBy(r => r.Position)
                .Select(Mapper.ToResultDto)
                .ToList();

            return Ok(ordered);
        }
    }
}
