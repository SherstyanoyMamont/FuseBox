using System.Security.Claims;
using FuseBox.App.DataBase;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace FuseBox.App.Services.Auth
{
    public sealed class AuthCookieEvents : CookieAuthenticationEvents
    {
        public const string SecurityStampClaim = "security_stamp";

        private readonly AppDbContext _context;

        public AuthCookieEvents(AppDbContext context)
        {
            _context = context;
        }

        public override async Task ValidatePrincipal(
            CookieValidatePrincipalContext context)
        {
            var userIdValue = context.Principal?
                .FindFirstValue(ClaimTypes.NameIdentifier);

            var cookieSecurityStamp = context.Principal?
                .FindFirstValue(SecurityStampClaim);

            if (!int.TryParse(userIdValue, out var userId) ||
                string.IsNullOrWhiteSpace(cookieSecurityStamp))
            {
                await RejectAsync(context);
                return;
            }

            var user = await _context.Users
                .AsNoTracking()
                .Where(candidate => candidate.Id == userId)
                .Select(candidate => new
                {
                    candidate.PasswordHash,
                    candidate.SecurityStamp
                })
                .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

            if (user == null ||
                string.IsNullOrWhiteSpace(user.PasswordHash) ||
                !string.Equals(
                    user.SecurityStamp,
                    cookieSecurityStamp,
                    StringComparison.Ordinal))
            {
                await RejectAsync(context);
            }
        }

        public override Task RedirectToLogin(
            RedirectContext<CookieAuthenticationOptions> context)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        public override Task RedirectToAccessDenied(
            RedirectContext<CookieAuthenticationOptions> context)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        private static async Task RejectAsync(
            CookieValidatePrincipalContext context)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(context.Scheme.Name);
        }
    }
}
