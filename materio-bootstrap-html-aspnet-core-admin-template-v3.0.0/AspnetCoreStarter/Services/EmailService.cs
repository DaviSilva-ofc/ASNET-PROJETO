using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using AspnetCoreStarter.Models;
using System.Threading.Tasks;

namespace AspnetCoreStarter.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = message };
            emailMessage.Body = bodyBuilder.ToMessageBody();

            try
            {
                using (var client = new SmtpClient())
                {
                    // For development, we might want to accept all SSL certificates (not recommended for production)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_emailSettings.SmtpUser, _emailSettings.SmtpPass);
                    await client.SendAsync(emailMessage);
                    await client.DisconnectAsync(true);
                }
            }
            catch (System.Exception ex)
            {
                // Fallback: Log to console so the user can see the email content (e.g. reset link) in the terminal
                System.Console.WriteLine("------ SIMULATED EMAIL START ------");
                System.Console.WriteLine($"Para: {email}");
                System.Console.WriteLine($"Assunto: {subject}");
                System.Console.WriteLine($"Corpo: {message}");
                System.Console.WriteLine("------ SIMULATED EMAIL END ------");
                System.Console.WriteLine($"Erro SMTP detalhado: {ex.Message}");

                // Do not throw in development if we want the user to see a "success" message but check the terminal
                // Or throw a clearer message. Let's throw a message that explains how to check the terminal.
                throw new System.Exception("O envio de email real falhou (verifique as credenciais no appsettings.json). No entanto, o link de recuperação foi impresso na consola/terminal do seu VS Code para que possa continuar a testar.", ex);
            }
        }
    }
}
