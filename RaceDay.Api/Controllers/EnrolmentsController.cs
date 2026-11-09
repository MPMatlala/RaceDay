using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    /// <summary>
    /// Participants enter events by choosing a category.
    /// </summary>
    [ApiController]
    [Route("api")]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public EnrolmentsController(RaceDayContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Enters the logged-in Participant into a category.
        /// </summary>
        /// <remarks>No body needed. The participantId comes from the token and a race number is generated.</remarks>
        /// <response code="201">Enrolled. Returns the enrolmentId and race number.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as an Organiser.</response>
        /// <response code="404">Category not found.</response>
        /// <response code="409">Already enrolled, or the category is full.</response>
        [HttpPost("categories/{categoryId:int}/enrolments")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Enrol(int categoryId)
        {
            int participantId = User.GetUserId();

            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
            {
                return NotFound();
            }

            bool alreadyEnrolled = await _context.Enrolments.AnyAsync(e =>
                e.ParticipantId == participantId && e.CategoryId == categoryId && e.Status != "Cancelled");
            if (alreadyEnrolled)
            {
                return Conflict("You are already enrolled in this category.");
            }

            int taken = await _context.Enrolments.CountAsync(e =>
                e.CategoryId == categoryId && e.Status != "Cancelled");
            if (taken >= category.MaxParticipants)
            {
                return Conflict("This category is full.");
            }

            // cancelled enrolments stay in the table, so the count never goes down and race numbers stay unique
            int totalEnrolments = await _context.Enrolments.CountAsync();

            var enrolment = new Enrolment
            {
                ParticipantId = participantId,
                CategoryId = categoryId,
                RaceNumber = "RD-" + (1001 + totalEnrolments),
                Status = "Confirmed"
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { enrolmentId = enrolment.EnrolmentId, raceNumber = enrolment.RaceNumber });
        }

        /// <summary>
        /// Lists all the events/categories the logged-in Participant has entered.
        /// </summary>
        /// <response code="200">An array of enrolments.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as an Organiser.</response>
        [HttpGet("participants/me/enrolments")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyEnrolments()
        {
            int participantId = User.GetUserId();

            var enrolments = await _context.Enrolments
                .Include(e => e.Participant)
                .Include(e => e.Category).ThenInclude(c => c.Event)
                .Where(e => e.ParticipantId == participantId)
                .ToListAsync();

            return Ok(enrolments.Select(Mapper.ToEnrolmentDto).ToList());
        }

        /// <summary>
        /// Cancels the logged-in Participant's own enrolment.
        /// </summary>
        /// <response code="204">Enrolment cancelled.</response>
        /// <response code="403">The enrolment belongs to another Participant (or the user is an Organiser).</response>
        /// <response code="404">Enrolment not found.</response>
        [HttpDelete("enrolments/{id:int}")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelEnrolment(int id)
        {
            var enrolment = await _context.Enrolments.FindAsync(id);

            if (enrolment == null)
            {
                return NotFound();
            }

            if (enrolment.ParticipantId != User.GetUserId())
            {
                return Forbid();
            }

            enrolment.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
