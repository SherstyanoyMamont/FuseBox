namespace FuseBox.App.Services.Auth
{
    public static class EmailNormalizer
    {
        public static string Canonicalize(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        public static string Normalize(string email)
        {
            return email.Trim().ToUpperInvariant();
        }
    }
}
