using System;
namespace Ignist.Data.EmailServices
{
	public interface IEmailService
	{
        Task SendEmailAsync(string to, string subject, string message);
    }
}

