using MimeKit;

namespace SportLab.Services
{
    public interface IEmailSender
    {
        Task SendAsync(MimeMessage message, string fromDisplayName, CancellationToken cancellationToken = default);
    }
}
