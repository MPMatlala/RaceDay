using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Helpers;

namespace RaceDay.Api.Controllers
{
    /// <summary>
    /// Organiser profile endpoints.
    /// </summary>
    [ApiController]
    [Route("api/organisers")]
    public class OrganisersController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public OrganisersController(RaceDayContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns the profile of the logged-in Organiser.
        /// </summary>
        /// <response code="200">The organiser profile.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as a Participant.</response>
        [HttpGet("me")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(typeof(OrganiserProfileDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyProfile()
        {
            int id = User.GetUserId();
            var organiser = await _context.Organisers.FindAsync(id);

            if (organiser == null)
            {
                return NotFound();
            }

            return Ok(new OrganiserProfileDto
            {
                OrganiserId = organiser.OrganiserId,
                FullName = organiser.FullName,
                Email = organiser.Email,
                PhoneNumber = organiser.PhoneNumber
            });
        }

        /// <summary>
        /// Lists every event the logged-in Organiser has created (past and upcoming), with enrolment totals.
        /// </summary>
        /// <remarks>Cancelled enrolments are not counted. Ordered by date, soonest first.</remarks>
        /// <response code="200">An array of event summaries.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as a Participant.</response>
        [HttpGet("me/events")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(typeof(List<EventSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyEvents()
        {
            int id = User.GetUserId();

            var events = await _context.Events
                .Include(e => e.Categories).ThenInclude(c => c.Enrolments)
                .Where(e => e.OrganiserId == id)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            var summaries = events.Select(e => new EventSummaryDto
            {
                EventId = e.EventId,
                OrganiserId = e.OrganiserId,
                EventName = e.EventName,
                EventDate = e.EventDate,
                Location = e.Location,
                Description = e.Description,
                Distance = e.Distance,
                EventType = e.EventType,
                BannerUrl = e.BannerUrl,
                CategoryCount = e.Categories.Count,
                TotalEnrolments = e.Categories.Sum(c => c.Enrolments.Count(en => en.Status != "Cancelled"))
            }).ToList();

            return Ok(summaries);
        }

        /// <summary>
        /// Updates the profile of the logged-in Organiser.
        /// </summary>
        /// <remarks>Expects fullName and phoneNumber.</remarks>
        /// <response code="200">The updated profile.</response>
        /// <response code="400">Invalid details.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as a Participant.</response>
        [HttpPut("me")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(typeof(OrganiserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateMyProfile(UpdateProfileDto dto)
        {
            int id = User.GetUserId();
            var organiser = await _context.Organisers.FindAsync(id);

            if (organiser == null)
            {
                return NotFound();
            }

            organiser.FullName = dto.FullName;
            organiser.PhoneNumber = dto.PhoneNumber;
            await _context.SaveChangesAsync();

            return Ok(new OrganiserProfileDto
            {
                OrganiserId = organiser.OrganiserId,
                FullName = organiser.FullName,
                Email = organiser.Email,
                PhoneNumber = organiser.PhoneNumber
            });
        }

        /// <summary>
        /// Gets the public profile of an Organiser (shown on an event page).
        /// </summary>
        /// <response code="200">The public profile (id and name only).</response>
        /// <response code="404">Organiser not found.</response>
        [HttpGet("{organiserId:int}")]
        [ProducesResponseType(typeof(OrganiserPublicDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrganiser(int organiserId)
        {
            var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.OrganiserId == organiserId);

            if (organiser == null)
            {
                return NotFound();
            }

            return Ok(new OrganiserPublicDto
            {
                OrganiserId = organiser.OrganiserId,
                FullName = organiser.FullName
            });
        }
    }
}
