using System.Security.Cryptography;
using FuseBox.App.DataBase;
using FuseBox.App.Models;
using FuseBox.App.Services.Auth;
using Microsoft.EntityFrameworkCore;

namespace FuseBox.App.Accounts
{
    /// <summary>
    /// Centralizes account security mutations. Any role/status change rotates
    /// SecurityStamp so already-issued authentication cookies stop being valid.
    /// </summary>
    public sealed class AccountService
    {
        private readonly AppDbContext _context;

        public AccountService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> PromoteToAdminAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            var normalizedEmail = EmailNormalizer.Normalize(email);

            var user = await _context.Users
                .FirstOrDefaultAsync(
                    candidate => candidate.NormalizedEmail == normalizedEmail,
                    cancellationToken);

            if (user == null)
            {
                return null;
            }

            await SetRoleAsync(user, UserRole.Admin, cancellationToken);
            return user;
        }

        public async Task SetRoleAsync(
            User user,
            UserRole role,
            CancellationToken cancellationToken = default)
        {
            if (user.Role == role)
            {
                return;
            }

            user.Role = role;
            InvalidateSessions(user);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task SetStatusAsync(
            User user,
            UserStatus status,
            CancellationToken cancellationToken = default)
        {
            if (user.Status == status)
            {
                return;
            }

            user.Status = status;
            InvalidateSessions(user);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task ForceLogoutAsync(
            User user,
            CancellationToken cancellationToken = default)
        {
            InvalidateSessions(user);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public static string CreateSecurityStamp()
        {
            return Convert.ToHexString(
                RandomNumberGenerator.GetBytes(32));
        }

        private static void InvalidateSessions(User user)
        {
            user.SecurityStamp = CreateSecurityStamp();
            user.UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}
