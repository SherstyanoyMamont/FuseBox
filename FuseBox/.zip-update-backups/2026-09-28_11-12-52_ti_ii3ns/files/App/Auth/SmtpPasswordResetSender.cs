using System.Net;
using System.Net.Mail;
using FuseBox.App.Models;

namespace FuseBox.App.Services.Auth
{
    public sealed class SmtpPasswordResetSender : IPasswordResetSender
    {
        private readonly IConfiguration _configuration;

        public SmtpPasswordResetSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendAsync(
            User user,
            string rawToken,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var host = _configuration["Email:Smtp:Host"];
            var from = _configuration["Email:Smtp:From"];
            var frontendBaseUrl =
                _configuration["Frontend:BaseUrl"]?.TrimEnd('/');

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(from) ||
                string.IsNullOrWhiteSpace(frontendBaseUrl))
            {
                throw new InvalidOperationException(
                    "Production password reset email is not configured.");
            }

            var port = int.TryParse(
                _configuration["Email:Smtp:Port"],
                out var configuredPort)
                ? configuredPort
                : 587;

            var enableSsl = !bool.TryParse(
                _configuration["Email:Smtp:EnableSsl"],
                out var configuredSsl) || configuredSsl;

            var username = _configuration["Email:Smtp:Username"];
            var password = _configuration["Email:Smtp:Password"];

            var resetUrl =
                $"{frontendBaseUrl}/reset-password?token=" +
                Uri.EscapeDataString(rawToken);

            using var message = new MailMessage
            {
                From = new MailAddress(from),
                Subject = "Reset your FuseBox password",
                Body =
                    "A password reset was requested for your FuseBox account.\n\n" +
                    $"Open this link to choose a new password:\n{resetUrl}\n\n" +
                    "If you did not request this, you can ignore this email.",
                IsBodyHtml = false
            };

            message.To.Add(new MailAddress(user.Email));

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl
            };

            if (!string.IsNullOrWhiteSpace(username))
            {
                client.Credentials = new NetworkCredential(
                    username,
                    password ?? string.Empty);
            }

            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(message);
        }
    }
}
