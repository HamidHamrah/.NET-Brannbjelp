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

        [JsonProperty("Role")]
        public string Role { get; set; } = "Normal"; // Default role

        [JsonProperty("PasswordResetToken")]
        public string PasswordResetToken { get; set; }

        [JsonProperty("PasswordResetTokenExpires")]
        public DateTime PasswordResetTokenExpires { get; set; }
    }
}

