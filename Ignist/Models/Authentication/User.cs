using System;
using Newtonsoft.Json;

namespace Ignist.Models
{
    public class User
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString(); 

        [JsonProperty("UserName")]
        public string UserName { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("passwordHash")]
        public string PasswordHash { get; set; }

        [JsonProperty("Roles")]
        public List<string> Roles { get; set; } = new List<string>();

    }
}

