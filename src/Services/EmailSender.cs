using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

namespace ChamadosTi.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(ILogger<EmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            _logger.LogInformation($"\n--- SIMULAÇÃO DE E-MAIL ---\nPara: {email}\nAssunto: {subject}\nLink: {htmlMessage}\n--------------------------\n");
            return Task.CompletedTask;
        }
    }
}
