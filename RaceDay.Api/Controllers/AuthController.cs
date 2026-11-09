using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    /// <summary>
    /// Register and log in as an Organiser or a Participant.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayContext _context;
        private readonly IConfiguration _config;

        public AuthController(RaceDayContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        /// <summary>
        /// Registers a new Organiser account.
        /// </summary>
        /// <remarks>Expects fullName, email, password and phoneNumber. The password is hashed before it is saved.</remarks>
        /// <response code="201">Account created. Returns the new organiserId.</response>
        /// <response code="400">Some of the details are missing or invalid.</response>
        /// <response code="409">The email is already registered.</response>
        [HttpPost("register/organiser")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RegisterOrganiser(RegisterDto dto)
        {
            if (await EmailExists(dto.Email))
            {
                return Conflict("This email is already registered.");
            }

            var organiser = new Organiser
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                PhoneNumber = dto.PhoneNumber
            };

            _context.Organisers.Add(organiser);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { message = "Organiser registered successfully.", organiserId = organiser.OrganiserId });
        }

        /// <summary>
        /// Registers a new Participant account.
        /// </summary>
        /// <remarks>Expects fullName, email, password and phoneNumber. The password is hashed before it is saved.</remarks>
        /// <response code="201">Account created. Returns the new participantId.</response>
        /// <response code="400">Some of the details are missing or invalid.</response>
        /// <response code="409">The email is already registered.</response>
        [HttpPost("register/participant")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RegisterParticipant(RegisterDto dto)
        {
            if (await EmailExists(dto.Email))
            {
                return Conflict("This email is already registered.");
            }

            var participant = new Participant
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                PhoneNumber = dto.PhoneNumber
            };

            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { message = "Participant registered successfully.", participantId = participant.ParticipantId });
        }

        /// <summary>
        /// Logs in an Organiser or a Participant.
        /// </summary>
        /// <remarks>Expects email and password. Returns a token, the user id and the role. Use the token in the Authorize button.</remarks>
        /// <response code="200">Login worked. Returns token, id and role.</response>
        /// <response code="401">Wrong email or password.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            // check organisers first
            var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.Email == dto.Email);
            if (organiser != null)
            {
                if (!PasswordIsCorrect(dto.Password, organiser.PasswordHash))
                {
                    return Unauthorized("Invalid email or password.");
                }

                return Ok(new LoginResponseDto
                {
                    Message = "Logged in successfully.",
                    Token = CreateToken(organiser.OrganiserId, "Organiser"),
                    Id = organiser.OrganiserId,
                    Role = "Organiser"
                });
            }

            // then participants
            var participant = await _context.Participants.FirstOrDefaultAsync(p => p.Email == dto.Email);
            if (participant != null)
            {
                if (!PasswordIsCorrect(dto.Password, participant.PasswordHash))
                {
                    return Unauthorized("Invalid email or password.");
                }

                return Ok(new LoginResponseDto
                {
                    Message = "Logged in successfully.",
                    Token = CreateToken(participant.ParticipantId, "Participant"),
                    Id = participant.ParticipantId,
                    Role = "Participant"
                });
            }

            return Unauthorized("Invalid email or password.");
        }

        // if the stored hash is not a real BCrypt hash (e.g. the fake ones from the SQL script)
        // Verify crashes, so we treat that as a wrong password instead of a 500 error
        private bool PasswordIsCorrect(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch (Exception)
            {
                return false;
            }
        }

        // an email can only be used once across both tables so login is never confusing
        private async Task<bool> EmailExists(string email)
        {
            bool inOrganisers = await _context.Organisers.AnyAsync(o => o.Email == email);
            bool inParticipants = await _context.Participants.AnyAsync(p => p.Email == email);
            return inOrganisers || inParticipants;
        }

        private string CreateToken(int id, string role)
        {
            var claims = new List<Claim>
            {
                new Claim("id", id.ToString()),
                new Claim("role", role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
