using System.Threading.Tasks;

namespace AspnetCoreStarter.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
