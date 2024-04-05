using Ignist.Data.Services;
using Ignist.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Ignist.Models.Authentication;


namespace Ignist.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ICosmosDbService _cosmosDbService;
        public AuthController(ICosmosDbService cosmosDbService)
        {
            _cosmosDbService = cosmosDbService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel registerModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _cosmosDbService.RegisterUserAsync(registerModel);
            if (result == "User registered.")
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
        {
            if (loginModel == null || string.IsNullOrWhiteSpace(loginModel.email) || string.IsNullOrWhiteSpace(loginModel.Password))
            {
                return BadRequest("Missing or invalid login details.");
            }

            var token = await _cosmosDbService.LoginUserAsync(loginModel);
            if (token == "User not found." || token == "Invalid password.")
            {
                return Unauthorized(token);
            }

            return Ok(token); // Anta at token er gyldig og send til klienten
        }


        [HttpPost("ForgotPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([Required] string email)
        {
            var result = await _cosmosDbService.HandleForgotPasswordAsync(email);
            if (result != null)
            {
                // Logikken her avhenger av hvordan du velger å håndtere e-postsending og suksessmelding.
                return Ok(new { message = $"Password change request is sent to {email}. Please check your email for the reset code." });
            }

            return NotFound(new { message = "User not registered." });
        }


        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var result = await _cosmosDbService.HandleResetPasswordAsync(request);
            if (result == "Password has been successfully reset.")
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
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

            var result = await _cosmosDbService.UpdateUserPasswordAsync(userEmail, model.OldPassword, model.NewPassword, model.ConfirmNewPassword);

            if (result == "Password updated successfully.")
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }





        [HttpGet("aboutme")]
        public async Task<IActionResult> AboutMe([FromQuery] string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("ID is required.");
            }

            var user = await _cosmosDbService.GetUserByIdAsync(id); // Updated to use GetUserByIdAsync
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


        [HttpDelete("delete-user/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                await _cosmosDbService.DeleteUserAsync(id);
                return Ok("User deleted successfully.");
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest($"An error occurred: {ex.Message}");
            }
        }


        [HttpPut("update-user/{userId}")] // Endret for å reflektere at vi nå bruker userId
        public async Task<IActionResult> UpdateUser(string userId, [FromBody] UserUpdateModel updateModel)
        {
            if (string.IsNullOrWhiteSpace(userId) || updateModel == null)
            {
                return BadRequest("User ID and update information are required.");
            }

            var response = await _cosmosDbService.UpdateUserAsync(userId, updateModel);
            if (response.Success)
            {
                return Ok(response.Message);
            }
            else
            {
                return BadRequest(response.Message);
            }
        }

    }
}
