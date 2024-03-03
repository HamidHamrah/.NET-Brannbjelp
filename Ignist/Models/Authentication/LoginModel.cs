using System;
using System.ComponentModel.DataAnnotations;

namespace Ignist.Models
{
    public class LoginModel
    {
        [Required]
        public string email { get; set; }

        public string Password { get; set; }
    }

}

