using System;
using System.ComponentModel.DataAnnotations;

namespace Ignist.Models.Authentication
{
	public class ResetPasswordRequest
	{
        [Required]
        public string Token { get; set; }

        [Required]
        public string Email { get; set; }


        [Required, MinLength(6, ErrorMessage = "Please enter at least 6 characters.")]
        public string NewPassword { get; set; }

        [Required, Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

    }
}

