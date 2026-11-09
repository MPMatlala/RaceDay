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
    /// Categories of an event (e.g. 10km, 21km, Under 20, Senior).
    /// </summary>
    [ApiController]
    [Route("api")]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public CategoriesController(RaceDayContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lists all the categories of an event.
        /// </summary>
        /// <response code="200">An array of categories.</response>
        /// <response code="404">Event not found.</response>
        [HttpGet("events/{eventId:int}/categories")]
        [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategories(int eventId)
        {
            bool eventExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
            if (!eventExists)
            {
                return NotFound();
            }

            var categories = await _context.Categories
                .Include(c => c.RaceRoute)
                .Where(c => c.EventId == eventId)
                .ToListAsync();

            return Ok(categories.Select(Mapper.ToCategoryDto).ToList());
        }

        /// <summary>
        /// Gets the details of one category.
        /// </summary>
        /// <response code="200">The category.</response>
        /// <response code="404">Category not found.</response>
        [HttpGet("categories/{id:int}")]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.RaceRoute)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            return Ok(Mapper.ToCategoryDto(category));
        }

        /// <summary>
        /// Adds a category to an event the logged-in Organiser owns.
        /// </summary>
        /// <remarks>Expects categoryName, distanceKm, entryFee and maxParticipants.</remarks>
        /// <response code="201">Category created. Returns the new categoryId.</response>
        /// <response code="400">Invalid details.</response>
        /// <response code="403">The event belongs to another Organiser (or the user is a Participant).</response>
        /// <response code="404">Event not found.</response>
        [HttpPost("events/{eventId:int}/categories")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateCategory(int eventId, CategoryCreateDto dto)
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

            var category = new Category
            {
                EventId = eventId,
                CategoryName = dto.CategoryName,
                DistanceKm = dto.DistanceKm,
                EntryFee = dto.EntryFee,
                MaxParticipants = dto.MaxParticipants
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { categoryId = category.CategoryId });
        }

        /// <summary>
        /// Updates a category on an event the logged-in Organiser owns.
        /// </summary>
        /// <remarks>Expects categoryName, distanceKm, entryFee and maxParticipants.</remarks>
        /// <response code="200">The updated category.</response>
        /// <response code="400">Invalid details.</response>
        /// <response code="403">The event belongs to another Organiser (or the user is a Participant).</response>
        /// <response code="404">Category not found.</response>
        [HttpPut("categories/{id:int}")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCategory(int id, CategoryCreateDto dto)
        {
            var category = await _context.Categories
                .Include(c => c.Event)
                .Include(c => c.RaceRoute)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            if (category.Event.OrganiserId != User.GetUserId())
            {
                return Forbid();
            }

            category.CategoryName = dto.CategoryName;
            category.DistanceKm = dto.DistanceKm;
            category.EntryFee = dto.EntryFee;
            category.MaxParticipants = dto.MaxParticipants;
            await _context.SaveChangesAsync();

            return Ok(Mapper.ToCategoryDto(category));
        }

        /// <summary>
        /// Removes a category that has no enrolments.
        /// </summary>
        /// <response code="204">Category deleted.</response>
        /// <response code="403">The event belongs to another Organiser (or the user is a Participant).</response>
        /// <response code="404">Category not found.</response>
        /// <response code="409">The category already has enrolments.</response>
        [HttpDelete("categories/{id:int}")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Event)
                .Include(c => c.RaceRoute)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            if (category.Event.OrganiserId != User.GetUserId())
            {
                return Forbid();
            }

            bool hasEnrolments = await _context.Enrolments.AnyAsync(e => e.CategoryId == id);
            if (hasEnrolments)
            {
                return Conflict("This category has enrolments and cannot be deleted.");
            }

            if (category.RaceRoute != null)
            {
                _context.RaceRoutes.Remove(category.RaceRoute);
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
