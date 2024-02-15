using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ignist.Models;

namespace Ignist.Data
{
    public interface IPublicationsRepository
    {
        Task<IEnumerable<Publication>> GetAllPublicationsAsync();
        Task<Publication> GetPublicationByIdAsync(string id, string UserId);
        Task AddPublicationAsync(Publication publication);
        Task UpdatePublicationAsync(Publication publication);
        Task DeletePublicationAsync(string id, string UserId);
    }
}
