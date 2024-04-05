using System;
namespace Ignist.Models.Authentication
{
	public class UserUpdateModel
    {
        public string UserName { get; set; }
        public string NewEmail { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }
    }
}

