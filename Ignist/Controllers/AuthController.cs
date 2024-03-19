using Ignist.Data;
using Ignist.Data.Services;
using Ignist.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Ignist.Data.EmailServices;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Ignist.Models.Authentication;


namespace Ignist.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ICosmosDbService _cosmosDbService;
        private readonly PasswordHelper _passwordHelper;
        private readonly JwtTokenService _jwtTokenService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;



        public AuthController(ICosmosDbService cosmosDbService, PasswordHelper passwordHelper, JwtTokenService jwtTokenService, IEmailService emailService, IConfiguration configuration)
        {
            _cosmosDbService = cosmosDbService;
            _passwordHelper = passwordHelper;
            _jwtTokenService = jwtTokenService;
            _emailService = emailService;
            _configuration = configuration;
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
                    PasswordHash = _passwordHelper.HashPassword(registerModel.Password)
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
            return Ok(token);
        }


        [HttpPost("ForgotPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([Required] string email)
        {
            var user = await _cosmosDbService.GetUserByEmailAsync(email);
            if (user != null)
            {
                // i denne delen generer vi tilgeldig token i databassen
                var token = await _cosmosDbService.GeneratePasswordResetTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

  
                var url = $"{_configuration["applicationUrl"]}/ResetPassword?email={email} AND THSI IS YOUT TOKEN token={encodedToken}";

                var emailMessage = $"<h1>Follow the instructions to reset your password</h1>" +
                                   $"<p>To reset your password <a href='{url}'>Click here</a></p>";

                await _emailService.SendEmailAsync(email, "Reset Password", emailMessage);

                return Ok(new { message = $"Password change request is sent to {user.Email}. Please check your email." });
            }

            return NotFound(new { message = "User not registered." });
        }


        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest("Invalid request.");
            }

            var user = await _cosmosDbService.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));

            // Valider token og sjekk at den ikke har utløpt
            if (user.PasswordResetToken != decodedToken || user.PasswordResetTokenExpires < DateTime.UtcNow)
            {
                return BadRequest("Invalid or expired token.");
            }

            try
            {
                // Oppdater brukerens passord
                user.PasswordHash = _passwordHelper.HashPassword(request.NewPassword);
                user.PasswordResetToken = null; // Fjern reset token
                user.PasswordResetTokenExpires = DateTime.MinValue; // Sett utløpstiden tilbake

                await _cosmosDbService.UpdateUserAsync(user);

                return Ok("Password has been successfully reset.");
            }
            catch (Exception ex)
            {
                return BadRequest($"An error occurred while resetting the password: {ex.Message}");
            }
        }

        [HttpGet("aboutme")]
        public async Task<IActionResult> AboutMe([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Email is required.");
            }

            var user = await _cosmosDbService.GetUserByEmailAsync(email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Return only safe and necessary user details
            return Ok(new { user.Id, user.UserName, user.Email });
        }
    }
}
