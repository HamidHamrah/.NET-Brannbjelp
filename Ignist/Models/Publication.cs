using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Ignist.Models
{
    public class Publication
    {
        // Bruker en string ID egnet for Cosmos DB
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [Required]
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [Required]
        [JsonProperty(PropertyName = "content")]
        public string Content { get; set; }

        [JsonProperty(PropertyName = "createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty(PropertyName = "updatedAt")]
        public DateTime UpdatedAt { get; set; }

        // Partition Key eksempel (du må bestemme hva som er mest egnet basert på din brukssak)
        [JsonProperty(PropertyName = "userId")]
        public string UserId { get; set; }


    }
}
