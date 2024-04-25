using Ignist.Data.Services;
using Ignist.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Ignist.Models.Authentication;
using Serilog;

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
            Log.Information("Received login request for {Email}", loginModel?.email);

            if (loginModel == null || string.IsNullOrWhiteSpace(loginModel.email) || string.IsNullOrWhiteSpace(loginModel.Password))
            {
                Log.Warning("Missing or invalid login details for {Email}", loginModel?.email);
                return BadRequest("Missing or invalid login details.");
            }

            Log.Debug("Attempting to authenticate user {Email}", loginModel.email);
            var token = await _cosmosDbService.LoginUserAsync(loginModel);

            if (token == "User not found." || token == "Invalid password.")
            {
                Log.Warning("Authentication failed for {Email}: {Reason}", loginModel.email, token);
                return Unauthorized(token);
            }

            Log.Information("User {Email} logged in successfully", loginModel.email);
            return Ok(token); // Assume token is valid and send to the client
        }


        [HttpPost("ForgotPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([Required] string email)
        {
            Log.Information("Initiating forgot password process for {Email}", email);

            var result = await _cosmosDbService.HandleForgotPasswordAsync(email);
            if (result != null)
            {
                // Assuming result contains non-sensitive information that can be logged
                Log.Information("Forgot password request processed successfully for {Email}. A reset email has been sent.", email);

                return Ok(new { message = $"Password change request is sent to {email}. Please check your email for the reset code." });
            }

            Log.Warning("Forgot password attempt for unregistered email: {Email}", email);
            return NotFound(new { message = "User not registered." });
        }


        [HttpPost("update-password")]
        [Authorize]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordModel model)
        {
            Log.Information("Attempt to update password initiated.");

            if (!ModelState.IsValid)
            {
                Log.Warning("Update password request has invalid model state.");
                return BadRequest(ModelState);
            }

            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                Log.Warning("Attempt to update password failed due to invalid token or missing email claim.");
                return Unauthorized("Invalid token.");
            }

            Log.Information("Updating password for {Email}", userEmail);
            var result = await _cosmosDbService.UpdateUserPasswordAsync(userEmail, model.OldPassword, model.NewPassword, model.ConfirmNewPassword);

            if (result == "Password updated successfully.")
            {
                Log.Information("Password updated successfully for {Email}", userEmail);
                return Ok(result);
            }
            else
            {
                Log.Warning("Failed to update password for {Email}: {Reason}", userEmail, result);
                return BadRequest(result);
            }
        }





        [HttpGet("aboutme")]
        public async Task<IActionResult> AboutMe([FromQuery] string id)
        {
            Log.Information("Received request for 'AboutMe' with ID {Id}", id);

            if (string.IsNullOrWhiteSpace(id))
            {
                Log.Warning("Attempt to access 'AboutMe' failed due to missing or empty ID.");
                return BadRequest("ID is required.");
            }

            Log.Debug("Looking up user by ID {Id}", id);
            var user = await _cosmosDbService.GetUserByIdAsync(id);
            if (user == null)
            {
                Log.Warning("User lookup failed for ID {Id}: User not found.", id);
                return NotFound("User not found.");
            }

            Log.Information("User found for ID {Id}: returning basic user details.", id);
            return Ok(new { user.Id, user.UserName, user.LastName, user.Email });
        }



        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            Log.Information("Initiating request to retrieve all users.");

            try
            {
                var users = await _cosmosDbService.GetAllUsersAsync();
                Log.Information("Successfully retrieved user data.");
                return Ok(users);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while retrieving users.");
                return BadRequest($"An error occurred while retrieving users: {ex.Message}");
            }
        }


        [HttpDelete("delete-user/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteUser(string id)
        {
            Log.Information("Request received to delete user with ID {Id}", id);

            try
            {
                await _cosmosDbService.DeleteUserAsync(id);
                Log.Information("User with ID {Id} deleted successfully.", id);
                return Ok("User deleted successfully.");
            }
            catch (ArgumentException ex)
            {
                Log.Warning(ex, "Failed to delete user with ID {Id}: {Message}", id, ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while deleting user with ID {Id}", id);
                return BadRequest($"An error occurred: {ex.Message}");
            }
        }


        [HttpPut("update-user/{userId}")] // Updated to reflect usage of userId
        [Authorize]
        public async Task<IActionResult> UpdateUser(string userId, [FromBody] UserUpdateModel updateModel)
        {
            Log.Information("Received update request for user with ID {UserId}", userId);

            if (string.IsNullOrWhiteSpace(userId) || updateModel == null)
            {
                Log.Warning("Update request failed for user with ID {UserId} due to missing or invalid input.", userId);
                return BadRequest("User ID and update information are required.");
            }

            Log.Debug("Attempting to update user with ID {UserId}", userId);
            var response = await _cosmosDbService.UpdateUserAsync(userId, updateModel);
            if (response.Success)
            {
                Log.Information("User with ID {UserId} updated successfully.", userId);
                return Ok(response.Message);
            }
            else
            {
                Log.Warning("Failed to update user with ID {UserId}: {Message}", userId, response.Message);
                return BadRequest(response.Message);
            }
        }


    }
}
