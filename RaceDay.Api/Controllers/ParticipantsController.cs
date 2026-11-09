using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Helpers;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers
{
    /// <summary>
    /// Participant profile endpoints.
    /// </summary>
    [ApiController]
    [Route("api/participants")]
    public class ParticipantsController : ControllerBase
    {
        private readonly RaceDayContext _context;
        private readonly IBlobService _blobs;

        public ParticipantsController(RaceDayContext context, IBlobService blobs)
        {
            _context = context;
            _blobs = blobs;
        }

        /// <summary>
        /// Returns the profile of the logged-in Participant.
        /// </summary>
        /// <response code="200">The participant profile.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as an Organiser.</response>
        [HttpGet("me")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(typeof(ParticipantProfileDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyProfile()
        {
            int id = User.GetUserId();
            var participant = await _context.Participants.FindAsync(id);

            if (participant == null)
            {
                return NotFound();
            }

            return Ok(ToDto(participant));
        }

        /// <summary>
        /// Updates the profile of the logged-in Participant.
        /// </summary>
        /// <remarks>Expects fullName and phoneNumber.</remarks>
        /// <response code="200">The updated profile.</response>
        /// <response code="400">Invalid details.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as an Organiser.</response>
        [HttpPut("me")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(typeof(ParticipantProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateMyProfile(UpdateProfileDto dto)
        {
            int id = User.GetUserId();
            var participant = await _context.Participants.FindAsync(id);

            if (participant == null)
            {
                return NotFound();
            }

            participant.FullName = dto.FullName;
            participant.PhoneNumber = dto.PhoneNumber;
            await _context.SaveChangesAsync();

            return Ok(ToDto(participant));
        }

        /// <summary>
        /// Uploads (or replaces) the profile picture of the logged-in Participant.
        /// </summary>
        /// <remarks>Send the image as multipart/form-data in a field called "file". JPG, PNG, GIF or WEBP, up to 5 MB. The image is stored in Azure Blob Storage.</remarks>
        /// <response code="200">Image stored. Returns the profilePictureUrl.</response>
        /// <response code="400">No file, wrong type or too large.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as an Organiser.</response>
        /// <response code="503">Azure Blob Storage is not available.</response>
        [HttpPost("me/picture")]
        [Authorize(Roles = "Participant")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> UploadPicture(IFormFile file)
        {
            int id = User.GetUserId();
            var participant = await _context.Participants.FindAsync(id);

            if (participant == null)
            {
                return NotFound();
            }

            string error = ImageValidator.Validate(file);
            if (error != null)
            {
                return BadRequest(error);
            }

            string newUrl;
            try
            {
                newUrl = await _blobs.UploadAsync("profiles/" + id, file);
            }
            catch (Exception)
            {
                return StatusCode(503, "Image storage is not available right now.");
            }

            string oldUrl = participant.ProfilePictureUrl;
            participant.ProfilePictureUrl = newUrl;
            await _context.SaveChangesAsync();

            await _blobs.DeleteAsync(oldUrl);

            return Ok(new { profilePictureUrl = newUrl });
        }

        /// <summary>
        /// Gets a participant's profile (used by an Organiser managing enrolments).
        /// </summary>
        /// <response code="200">The participant profile.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as a Participant.</response>
        /// <response code="404">Participant not found.</response>
        [HttpGet("{participantId:int}")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(typeof(ParticipantProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetParticipant(int participantId)
        {
            var participant = await _context.Participants.FindAsync(participantId);

            if (participant == null)
            {
                return NotFound();
            }

            return Ok(ToDto(participant));
        }

        private ParticipantProfileDto ToDto(Models.Participant p)
        {
            return new ParticipantProfileDto
            {
                ParticipantId = p.ParticipantId,
                FullName = p.FullName,
                Email = p.Email,
                PhoneNumber = p.PhoneNumber,
                ProfilePictureUrl = p.ProfilePictureUrl
            };
        }
    }
}