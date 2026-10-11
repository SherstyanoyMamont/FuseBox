using FuseBox.App.Models;

namespace FuseBox.App.Services.Auth
{
    public interface IPasswordResetSender
    {
        Task SendAsync(
            User user,
            string rawToken,
            CancellationToken cancellationToken = default);
    }
}
