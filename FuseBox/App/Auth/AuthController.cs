using System.Security.Claims;
using System.Security.Cryptography;
using FuseBox.App.Contracts.Auth;
using FuseBox.App.Contracts.Common;
using FuseBox.App.DataBase;
using FuseBox.App.Models;
using FuseBox.App.Services.Auth;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FuseBox.App.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController : ControllerBase
    {
        private static readonly TimeSpan ResetTokenLifetime =
            TimeSpan.FromMinutes(30);

        private readonly AppDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IAntiforgery _antiforgery;
        private readonly IPasswordResetSender _passwordResetSender;
        private readonly ILogger<AuthController> _logger;
        private readonly string _dummyPasswordHash;

        public AuthController(
            AppDbContext context,
            IPasswordHasher<User> passwordHasher,
            IAntiforgery antiforgery,
            IPasswordResetSender passwordResetSender,
            ILogger<AuthController> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _antiforgery = antiforgery;
            _passwordResetSender = passwordResetSender;
            _logger = logger;

            _dummyPasswordHash = _passwordHasher.HashPassword(
                new User(),
                Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
        }

        [HttpGet("csrf")]
        [AllowAnonymous]
        public ActionResult<CsrfTokenResponse> GetCsrfToken()
        {
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

            if (string.IsNullOrWhiteSpace(tokens.RequestToken))
            {
                return Problem(
                    title: "Unable to create CSRF token.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            return Ok(new CsrfTokenResponse
            {
                Token = tokens.RequestToken
            });
        }

        [HttpPost("register")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationFailed();
            }

            var email = EmailNormalizer.Canonicalize(request.Email);
            var normalizedEmail = EmailNormalizer.Normalize(request.Email);

            if (await _context.Users.AnyAsync(
                user => user.NormalizedEmail == normalizedEmail,
                cancellationToken))
            {
                return Conflict(Error(
                    ApiErrorCodes.EmailAlreadyInUse,
                    "An account with this email already exists."));
            }

            var now = DateTime.UtcNow;
            var user = new User
            {
                Email = email,
                NormalizedEmail = normalizedEmail,
                SecurityStamp = NewSecurityStamp(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                request.Password);

            _context.Users.Add(user);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Covers a concurrent registration that passed the first check.
                if (await _context.Users.AsNoTracking().AnyAsync(
                    candidate => candidate.NormalizedEmail == normalizedEmail,
                    cancellationToken))
                {
                    return Conflict(Error(
                        ApiErrorCodes.EmailAlreadyInUse,
                        "An account with this email already exists."));
                }

                throw;
            }

            await SignInAsync(user);

            return Created(
                "/api/auth/me",
                ToResponse(user));
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationFailed();
            }

            var normalizedEmail = EmailNormalizer.Normalize(request.Email);

            var user = await _context.Users
                .FirstOrDefaultAsync(
                    candidate =>
                        candidate.NormalizedEmail == normalizedEmail,
                    cancellationToken);

            if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                // Keep the failure path computationally similar without
                // revealing whether the email exists.
                _passwordHasher.VerifyHashedPassword(
                    new User(),
                    _dummyPasswordHash,
                    request.Password);

                return InvalidCredentials();
            }

            var verification = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);

            if (verification == PasswordVerificationResult.Failed)
            {
                return InvalidCredentials();
            }

            if (verification ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(
                    user,
                    request.Password);
                user.UpdatedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            await SignInAsync(user);
            return Ok(ToResponse(user));
        }

        [HttpPost("logout")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return NoContent();
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            if (!userId.HasValue)
            {
                return AuthenticationRequired();
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    candidate => candidate.Id == userId.Value,
                    cancellationToken);

            if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                await HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);

                return AuthenticationRequired();
            }

            return Ok(ToResponse(user));
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("password-reset")]
        public async Task<IActionResult> ForgotPassword(
            [FromBody] ForgotPasswordRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationFailed();
            }

            var normalizedEmail = EmailNormalizer.Normalize(request.Email);

            var user = await _context.Users
                .FirstOrDefaultAsync(
                    candidate =>
                        candidate.NormalizedEmail == normalizedEmail,
                    cancellationToken);

            if (user != null && !string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                var now = DateTime.UtcNow;
                var rawToken = PasswordResetTokens.CreateRawToken();

                var token = new PasswordResetToken
                {
                    User = user,
                    TokenHash = PasswordResetTokens.Hash(rawToken),
                    CreatedAtUtc = now,
                    ExpiresAtUtc = now.Add(ResetTokenLifetime)
                };

                _context.PasswordResetTokens.Add(token);
                await _context.SaveChangesAsync(cancellationToken);

                try
                {
                    await _passwordResetSender.SendAsync(
                        user,
                        rawToken,
                        cancellationToken);
                }
                catch (Exception exception)
                {
                    // The public response stays generic to avoid account
                    // enumeration. The raw token is intentionally not logged.
                    _logger.LogError(
                        exception,
                        "Unable to deliver password reset email for user {UserId}.",
                        user.Id);
                }
            }

            // Same response whether the account exists or not.
            return Accepted();
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("password-reset")]
        public async Task<IActionResult> ResetPassword(
            [FromBody] ResetPasswordRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationFailed();
            }

            var tokenHash = PasswordResetTokens.Hash(request.Token);
            var now = DateTime.UtcNow;

            var resetToken = await _context.PasswordResetTokens
                .Include(token => token.User)
                .FirstOrDefaultAsync(
                    token => token.TokenHash == tokenHash,
                    cancellationToken);

            if (resetToken == null || resetToken.UsedAtUtc.HasValue)
            {
                return BadRequest(Error(
                    ApiErrorCodes.InvalidResetToken,
                    "The password reset token is invalid."));
            }

            if (resetToken.ExpiresAtUtc <= now)
            {
                return BadRequest(Error(
                    ApiErrorCodes.ResetTokenExpired,
                    "The password reset token has expired."));
            }

            var user = resetToken.User;

            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                request.NewPassword);
            user.SecurityStamp = NewSecurityStamp();
            user.UpdatedAtUtc = now;

            var outstandingTokens = await _context.PasswordResetTokens
                .Where(token =>
                    token.UserId == user.Id &&
                    !token.UsedAtUtc.HasValue)
                .ToListAsync(cancellationToken);

            foreach (var token in outstandingTokens)
            {
                token.UsedAtUtc = now;
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (GetCurrentUserId() == user.Id)
            {
                await HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
            }

            return NoContent();
        }

        private async Task SignInAsync(User user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(AuthCookieEvents.SecurityStampClaim, user.SecurityStamp)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    AllowRefresh = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : null;
        }

        private IActionResult InvalidCredentials()
        {
            return Unauthorized(Error(
                ApiErrorCodes.InvalidCredentials,
                "Invalid email or password."));
        }

        private IActionResult AuthenticationRequired()
        {
            return Unauthorized(Error(
                ApiErrorCodes.AuthenticationRequired,
                "Authentication is required."));
        }

        private IActionResult ValidationFailed()
        {
            var errors = ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(error =>
                            string.IsNullOrWhiteSpace(error.ErrorMessage)
                                ? "Invalid value."
                                : error.ErrorMessage)
                        .ToArray());

            return BadRequest(new ApiErrorResponse
            {
                Code = ApiErrorCodes.ValidationFailed,
                Message = "Validation failed.",
                Errors = errors
            });
        }

        private static ApiErrorResponse Error(
            string code,
            string message)
        {
            return new ApiErrorResponse
            {
                Code = code,
                Message = message
            };
        }

        private static CurrentUserResponse ToResponse(User user)
        {
            return new CurrentUserResponse
            {
                Id = user.Id,
                Email = user.Email
            };
        }

        private static string NewSecurityStamp()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        }
    }
}
