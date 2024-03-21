using Ignist.Data;
using Ignist.Data.Services;
using Ignist.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Ignist.Data.EmailServices;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Security.Claims;
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
                // Generer tilfeldig kode
                var random = new Random();
                var code = random.Next(100000, 999999).ToString(); // Genererer en 6-sifret kode

                // Lagre kode og utløpstid i databasen
                user.PasswordResetCode = code;
                user.PasswordResetCodeExpires = DateTime.UtcNow.AddHours(1); // Koden utløper om 1 time
                await _cosmosDbService.UpdateUserAsync(user);

                // Send e-post med koden
                var emailMessage = $"<h1>Follow the instructions to reset your password</h1>" +
                                   $"<p>Your password reset code is: {code}</p>";

                await _emailService.SendEmailAsync(email, "Reset Password", emailMessage);

                return Ok(new { message = $"Password change request is sent to {user.Email}. Please check your email for the reset code." });
            }

            return NotFound(new { message = "User not registered." });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var user = await _cosmosDbService.GetUserByEmailAsync(request.Email);
            if (user == null || user.PasswordResetCodeExpires < DateTime.UtcNow || user.PasswordResetCode != request.Code)
            {
                return BadRequest("Invalid or expired code.");
            }

            try
            {
                // Oppdater brukerens passord
                user.PasswordHash = _passwordHelper.HashPassword(request.NewPassword);
                user.PasswordResetCode = null; // Fjern kode
                user.PasswordResetCodeExpires = DateTime.MinValue; // Sett utløpstiden tilbake

                await _cosmosDbService.UpdateUserAsync(user);

                return Ok("Password has been successfully reset.");
            }
            catch (Exception ex)
            {
                return BadRequest($"An error occurred while resetting the password: {ex.Message}");
            }
        }

        [HttpPost("update-password")]
        [Authorize]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("Invalid token.");
            }

            var user = await _cosmosDbService.GetUserByEmailAsync(userEmail);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var result = _passwordHelper.VerifyPassword(user.PasswordHash, model.OldPassword);
            if (result == PasswordVerificationResult.Failed)
            {
                return BadRequest("Old password is incorrect.");
            }

            if (!model.NewPassword.Equals(model.ConfirmNewPassword))
            {
                return BadRequest("The new password and confirmation password do not match.");
            }

            // Her legger vi til en ekstra sjekk for å sikre at det nye passordet oppfyller kompleksitetskravene
            if (!_passwordHelper.ValidatePassword(model.NewPassword))
            {
                return BadRequest("New password does not meet complexity requirements.");
            }

            user.PasswordHash = _passwordHelper.HashPassword(model.NewPassword);
            await _cosmosDbService.UpdateUserAsync(user);

            return Ok("Password updated successfully.");
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

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _cosmosDbService.GetAllUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest($"An error occurred while retrieving users: {ex.Message}");
            }
        }
    }
}
