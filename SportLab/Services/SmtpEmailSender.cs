using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace SportLab.Services
{
    public sealed class SmtpEmailSender : IEmailSender
    {
        private readonly EmailOptions _options;

        public SmtpEmailSender(IOptions<EmailOptions> options)
        {
            _options = options.Value;
        }

        public async Task SendAsync(MimeMessage message, string fromDisplayName, CancellationToken cancellationToken = default)
        {
            var fromAddress = string.IsNullOrWhiteSpace(_options.FromAddress)
                ? _options.UserName
                : _options.FromAddress;

            if (string.IsNullOrWhiteSpace(fromAddress))
            {
                throw new InvalidOperationException("Email from address is not configured.");
            }

            message.From.Add(new MailboxAddress(fromDisplayName, fromAddress));

            using var client = new SmtpClient();
            await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, SecureSocketOptions.StartTls, cancellationToken);
            await client.AuthenticateAsync(_options.UserName, _options.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}
