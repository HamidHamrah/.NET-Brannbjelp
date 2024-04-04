using System;
using System.Threading.Tasks;
using Ignist.Models;
using Ignist.Models.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using User = Ignist.Models.User;
using Ignist.Services;
using Ignist.Data.EmailServices;

namespace Ignist.Data.Services
{
    public class CosmosDbService : ICosmosDbService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;
        private readonly PasswordHelper _passwordHelper;
        private readonly JwtTokenService _jwtTokenService;
        private readonly IEmailService _emailService;

        public CosmosDbService(CosmosClient cosmosClient, IConfiguration configuration, PasswordHelper passwordHelper, JwtTokenService jwtTokenService, IEmailService emailService)
        {
            _cosmosClient = cosmosClient;
            var databaseName = configuration["CosmosDbSettings:DatabaseName"];
            var containerName = "User3"; 
            _container = _cosmosClient.GetContainer(databaseName, containerName);
            _passwordHelper = passwordHelper;
            _jwtTokenService = jwtTokenService;
            _emailService = emailService;
        }

        // Henter en bruker basert på brukernavn, som er partition key
        public async Task<User> GetUserByEmailAsync(string email)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.email = @email").WithParameter("@email", email);
            var iterator = _container.GetItemQueryIterator<User>(query);

            List<User> matches = new List<User>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                matches.AddRange(response.Resource); // Pass på at du bruker .Resource
            }

            return matches.FirstOrDefault();
        }

        public async Task<User> GetUserByIdAsync(string userId)
        {
            try
            {
                return await _container.ReadItemAsync<User>(userId, new PartitionKey(userId));
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<string> RegisterUserAsync(RegisterModel registerModel)
        {
            var userExists = await GetUserByEmailAsync(registerModel.Email);
            if (userExists != null)
            {
                return "User already exists.";
            }

            try
            {
                var user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = registerModel.UserName,
                    Email = registerModel.Email,
                    PasswordHash = _passwordHelper.HashPassword(registerModel.Password)
                };

                await AddUserAsync(user);
                return "User registered.";
            }
            catch (Exception ex)
            {
                // Log exception her
                return $"An error occurred: {ex.Message}";
            }
        }

        public async Task<string> LoginUserAsync(LoginModel loginModel)
        {
            var user = await GetUserByEmailAsync(loginModel.email);
            if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return "User not found.";
            }

            var result = _passwordHelper.VerifyPassword(user.PasswordHash, loginModel.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                return "Invalid password.";
            }

            var token = _jwtTokenService.GenerateToken(user);
            return token; // Dette antar at token er en streng. Juster etter faktisk implementasjon.
        }

        public async Task<string> HandleForgotPasswordAsync(string email)
        {
            var user = await GetUserByEmailAsync(email);
            if (user != null)
            {
                var random = new Random();
                var code = random.Next(100000, 999999).ToString();

                user.PasswordResetCode = code;
                user.PasswordResetCodeExpires = DateTime.UtcNow.AddHours(1);
                await UpdateUserAsync(user);

                // Opprett e-postmeldingen
                var emailSubject = "Reset Your Password";
                var emailMessage = $"<p>Your password reset code is: {code}</p><p>This code will expire in 1 hour.</p>";

                // Send e-post
                await _emailService.SendEmailAsync(email, emailSubject, emailMessage);

                // Returner suksessmeldingen
                return "Password reset code sent."; // Du kan velge å returnere koden eller en suksessmelding
            }

            return "User not found."; // Eller en annen feilmelding om brukeren ikke finnes
        }

        public async Task<string> HandleResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await GetUserByEmailAsync(request.Email);
            if (user == null || user.PasswordResetCodeExpires < DateTime.UtcNow || user.PasswordResetCode != request.Code)
            {
                return "Invalid or expired code.";
            }

            try
            {
                user.PasswordHash = _passwordHelper.HashPassword(request.NewPassword);
                user.PasswordResetCode = null;
                user.PasswordResetCodeExpires = DateTime.MinValue;

                await UpdateUserAsync(user);

                return "Password has been successfully reset.";
            }
            catch (Exception ex)
            {
                return $"An error occurred while resetting the password: {ex.Message}";
            }
        }

        public async Task<string> UpdateUserPasswordAsync(string userEmail, string oldPassword, string newPassword, string confirmNewPassword)
        {
            var user = await GetUserByEmailAsync(userEmail);
            if (user == null)
            {
                return "User not found.";
            }

            var result = _passwordHelper.VerifyPassword(user.PasswordHash, oldPassword);
            if (result == PasswordVerificationResult.Failed)
            {
                return "Old password is incorrect.";
            }

            if (!newPassword.Equals(confirmNewPassword))
            {
                return "The new password and confirmation password do not match.";
            }

            // Ekstra sjekk for passordkompleksitet
            if (!_passwordHelper.ValidatePassword(newPassword))
            {
                return "New password does not meet complexity requirements.";
            }

            user.PasswordHash = _passwordHelper.HashPassword(newPassword);
            await UpdateUserAsync(user);

            return "Password updated successfully.";
        }

        public async Task<ServiceResponse> UpdateUserAsync(string currentEmail, UserUpdateModel updateModel)
        {
            try
            {
                // Hent eksisterende bruker basert på e-post eller id
                var user = await GetUserByEmailAsync(currentEmail); // Eller bruk GetUserByIdAsync hvis du oppdaterer via id
                if (user == null)
                {
                    return new ServiceResponse { Success = false, Message = "User not found." };
                }

                // Oppdater brukerobjektet med nye verdier
                user.UserName = updateModel.UserName ?? user.UserName;
                user.Role = updateModel.Role ?? user.Role;

                // Anta at e-postadresse kan endres, og sjekk for potensielle konflikter
                if (!string.IsNullOrWhiteSpace(updateModel.NewEmail) && updateModel.NewEmail != user.Email)
                {
                    var emailExists = await GetUserByEmailAsync(updateModel.NewEmail);
                    if (emailExists != null)
                    {
                        return new ServiceResponse { Success = false, Message = "The new email is already in use." };
                    }
                    user.Email = updateModel.NewEmail;
                }

                // Oppdater dokumentet i Cosmos DB
                await _container.ReplaceItemAsync(user, user.Id, new PartitionKey(user.Id));

                return new ServiceResponse { Success = true, Message = "User updated successfully." };
            }
            catch (CosmosException ex)
            {
                // Logg feilen eller håndter den på egnet måte
                return new ServiceResponse { Success = false, Message = $"An error occurred: {ex.Message}" };
            }
        }

        // metoden for å lage ny bruker
        public async Task AddUserAsync(User user)
        {
            await _container.CreateItemAsync(user, new PartitionKey(user.Id));
        }

        public async Task UpdateUserAsync(User user)
        {
            await _container.UpsertItemAsync(user, new PartitionKey(user.Id));
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            var container = _cosmosClient.GetContainer("data3900", "User3"); //her må man oppgi manuelt database navn og container
            var query = "SELECT * FROM c"; 
            var queryIterator = container.GetItemQueryIterator<User>(query);
            var users = new List<User>();

            while (queryIterator.HasMoreResults)
            {
                var response = await queryIterator.ReadNextAsync();
                users.AddRange(response.ToList());
            }

            return users;
        }

        public async Task DeleteUserAsync(string id)
        {
            try
            {
                await _container.DeleteItemAsync<User>(id, new PartitionKey(id));
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Dette kan logges eller håndteres videre om nødvendig.
                throw new ArgumentException($"No user found with id: {id}", ex);
            }
        }
    }
}
