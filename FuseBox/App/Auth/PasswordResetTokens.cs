using System.Security.Cryptography;
using System.Text;

namespace FuseBox.App.Services.Auth
{
    public static class PasswordResetTokens
    {
        public static string CreateRawToken()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        }

        public static string Hash(string rawToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexString(bytes);
        }
    }
}
