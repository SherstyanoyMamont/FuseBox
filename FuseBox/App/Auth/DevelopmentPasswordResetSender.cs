using FuseBox.App.Models;

namespace FuseBox.App.Services.Auth
{
    public sealed class DevelopmentPasswordResetSender : IPasswordResetSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DevelopmentPasswordResetSender> _logger;

        public DevelopmentPasswordResetSender(
            IConfiguration configuration,
            ILogger<DevelopmentPasswordResetSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public Task SendAsync(
            User user,
            string rawToken,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var frontendBaseUrl =
                _configuration["Frontend:BaseUrl"]?.TrimEnd('/') ??
                "http://localhost:3000";

            var resetUrl =
                $"{frontendBaseUrl}/reset-password?token=" +
                Uri.EscapeDataString(rawToken);

            // Development only. Program.cs never registers this sender outside
            // the Development environment.
            _logger.LogWarning(
                "DEV password reset for {Email}: {ResetUrl}",
                user.Email,
                resetUrl);

            return Task.CompletedTask;
        }
    }
}
