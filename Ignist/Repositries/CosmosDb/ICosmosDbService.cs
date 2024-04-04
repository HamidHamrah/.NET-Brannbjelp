using System;
using Ignist.Models;
using System.Threading.Tasks;
using Ignist.Models.Authentication;
using Ignist.Services;

namespace Ignist.Data.Services
{
	public interface ICosmosDbService
	{
        Task<User> GetUserByEmailAsync(string email);
        Task<User> GetUserByIdAsync(string userId);
        Task AddUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task DeleteUserAsync(string email);
        Task<string> RegisterUserAsync(RegisterModel registerModel);
        Task<string> LoginUserAsync(LoginModel loginModel);
        Task<string> HandleForgotPasswordAsync(string email);
        Task<string> HandleResetPasswordAsync(ResetPasswordRequest request);
        Task<string> UpdateUserPasswordAsync(string userEmail, string oldPassword, string newPassword, string confirmNewPassword);
        Task<ServiceResponse> UpdateUserAsync(string currentEmail, UserUpdateModel updateModel);

    }
}

