using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;

namespace Ignist.Data.EmailServices
{
    public class SendGridEmailService : IEmailService
    {
        private readonly string _sendGridApiKey;

        public SendGridEmailService(IConfiguration configuration)
        {
            _sendGridApiKey = configuration["SendGridApiKey"];
        }

        public async Task SendEmailAsync(string toEmail, string emailSubject, string emailMessage)
        {
            var client = new SendGridClient(_sendGridApiKey); 
            var from = new EmailAddress("zolfeqarshirzadehg@gmail.com", "Zulfeqar");
            var to = new EmailAddress(toEmail, "Recipient Name"); //Her kan du velge mottakerens navn valgfritt
            var subject = emailSubject;
            var plainTextContent = emailMessage;
            var htmlContent = emailMessage; 
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);

            if (response.StatusCode != System.Net.HttpStatusCode.OK && response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                throw new Exception($"Failed to send email. Status code: {response.StatusCode}");
            }
        }
    }
}
