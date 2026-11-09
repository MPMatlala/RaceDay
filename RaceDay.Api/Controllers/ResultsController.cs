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
    /// Finish times and positions.
    /// </summary>
    [ApiController]
    [Route("api")]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public ResultsController(RaceDayContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Captures the finish result for an enrolment.
        /// </summary>
        /// <remarks>Expects finishTime (e.g. "01:52:30"), chipTime and position. Only the Organiser who owns the event can do this.</remarks>
        /// <response code="201">Result captured. Returns the resultId.</response>
        /// <response code="400">Invalid details, or the enrolment was cancelled.</response>
        /// <response code="403">The event belongs to another Organiser (or the user is a Participant).</response>
        /// <response code="404">Enrolment not found.</response>
        /// <response code="409">This enrolment already has a result.</response>
        [HttpPost("enrolments/{enrolmentId:int}/results")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CaptureResult(int enrolmentId, ResultCreateDto dto)
        {
            var enrolment = await _context.Enrolments
                .Include(e => e.Category).ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);

            if (enrolment == null)
            {
                return NotFound();
            }

            if (enrolment.Category.Event.OrganiserId != User.GetUserId())
            {
                return Forbid();
            }

            if (enrolment.Status == "Cancelled")
            {
                return BadRequest("This enrolment was cancelled.");
            }

            bool resultExists = await _context.Results.AnyAsync(r => r.EnrolmentId == enrolmentId);
            if (resultExists)
            {
                return Conflict("A result has already been captured for this enrolment.");
            }

            var result = new Result
            {
                EnrolmentId = enrolmentId,
                FinishTime = dto.FinishTime,
                ChipTime = dto.ChipTime,
                Position = dto.Position
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { resultId = result.ResultId });
        }

        /// <summary>
        /// Corrects a result that was captured earlier.
        /// </summary>
        /// <remarks>Expects finishTime, chipTime and position. Only the Organiser who owns the event can do this.</remarks>
        /// <response code="200">The updated result.</response>
        /// <response code="400">Invalid details.</response>
        /// <response code="403">The event belongs to another Organiser (or the user is a Participant).</response>
        /// <response code="404">Result not found.</response>
        [HttpPut("results/{id:int}")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateResult(int id, ResultCreateDto dto)
        {
            var result = await _context.Results
                .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
                .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null)
            {
                return NotFound();
            }

            if (result.Enrolment.Category.Event.OrganiserId != User.GetUserId())
            {
                return Forbid();
            }

            result.FinishTime = dto.FinishTime;
            result.ChipTime = dto.ChipTime;
            result.Position = dto.Position;
            await _context.SaveChangesAsync();

            return Ok(Mapper.ToResultDto(result));
        }

        /// <summary>
        /// Returns the logged-in Participant's own results.
        /// </summary>
        /// <response code="200">An array of the participant's results.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as an Organiser.</response>
        [HttpGet("participants/me/results")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyResults()
        {
            int participantId = User.GetUserId();

            var results = await _context.Results
                .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
                .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event)
                .Where(r => r.Enrolment.ParticipantId == participantId)
                .OrderByDescending(r => r.Enrolment.Category.Event.EventDate)
                .ToListAsync();

            // how many people finished in each of the participant's categories (for "47th out of 312")
            var categoryIds = results.Select(r => r.Enrolment.CategoryId).Distinct().ToList();
            var finisherCounts = await _context.Results
                .Where(r => categoryIds.Contains(r.Enrolment.CategoryId))
                .GroupBy(r => r.Enrolment.CategoryId)
                .Select(g => new { CategoryId = g.Key, Total = g.Count() })
                .ToListAsync();

            var dtos = results.Select(Mapper.ToResultDto).ToList();
            for (int i = 0; i < results.Count; i++)
            {
                var match = finisherCounts.FirstOrDefault(f => f.CategoryId == results[i].Enrolment.CategoryId);
                dtos[i].TotalFinishers = match == null ? 0 : match.Total;
            }

            return Ok(dtos);
        }
    }
}