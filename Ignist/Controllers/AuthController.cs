using Ignist.Data;
using Ignist.Data.Services;
using Ignist.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;


namespace Ignist.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ICosmosDbService _cosmosDbService;
        private readonly PasswordHelper _passwordHelper;
        private readonly JwtTokenService _jwtTokenService;

        public AuthController(ICosmosDbService cosmosDbService, PasswordHelper passwordHelper, JwtTokenService jwtTokenService)
        {
            _cosmosDbService = cosmosDbService;
            _passwordHelper = passwordHelper;
            _jwtTokenService = jwtTokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel registerModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userExists = await _cosmosDbService.GetUserByEmailAsync(registerModel.UserName);
            if (userExists != null)
            {
                return BadRequest("User already exists.");
            }

            // Sjekk om e-post allerede er registrert

            try
            {
                var user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = registerModel.UserName,
                    Email = registerModel.Email,
                    PasswordHash = _passwordHelper.HashPassword(registerModel.Password),
                    Roles = new List<string> { "User" }
                };

                await _cosmosDbService.AddUserAsync(user);
                return Ok("User registered.");
            }
            catch (Exception ex)
            {
                return BadRequest($"An error occurred: {ex.Message}");
            }
        }



        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
        {
            if (loginModel == null || string.IsNullOrWhiteSpace(loginModel.email) || string.IsNullOrWhiteSpace(loginModel.Password))
            {
                return BadRequest("Missing or invalid login details.");
            }

            if (_cosmosDbService == null) return Problem("Database service is not available.");

            var user = await _cosmosDbService.GetUserByEmailAsync(loginModel.email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (_passwordHelper == null) return Problem("Password helper service is not available.");

            var result = _passwordHelper.VerifyPassword(user.PasswordHash, loginModel.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Invalid password.");
            }

            if (_jwtTokenService == null) return Problem("Token service is not available.");

            // Generer JWT-token
            var token = _jwtTokenService.GenerateToken(user);
            return Ok(new { token = token });
        }

    }
}

