using System;
using Ignist.Models;
using System.Threading.Tasks;

namespace Ignist.Data.Services
{
	public interface ICosmosDbService
	{
        Task<User> GetUserByEmailAsync(string email);

        Task AddUserAsync(User user);
       
    }
}

