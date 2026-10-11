using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace FuseBox.App.Billing
{
    public sealed class PaddleWebhookVerifier
    {
        private readonly PaddleBillingOptions _options;

        public PaddleWebhookVerifier(
            IOptions<PaddleBillingOptions> options)
        {
            _options = options.Value;
        }

        public bool Verify(
            string? signatureHeader,
            byte[] rawBody,
            DateTimeOffset nowUtc)
        {
            if (!_options.IsWebhookConfigured ||
                string.IsNullOrWhiteSpace(signatureHeader))
            {
                return false;
            }

            long? timestamp = null;
            var signatures = new List<string>();

            foreach (var part in signatureHeader.Split(';'))
            {
                var separator = part.IndexOf('=');
                if (separator <= 0 || separator == part.Length - 1)
                {
                    continue;
                }

                var key = part[..separator].Trim();
                var value = part[(separator + 1)..].Trim();

                if (key == "ts" && long.TryParse(value, out var parsed))
                {
                    timestamp = parsed;
                }
                else if (key == "h1" && value.Length > 0)
                {
                    signatures.Add(value);
                }
            }

            if (!timestamp.HasValue || signatures.Count == 0)
            {
                return false;
            }

            var signedAt = DateTimeOffset.FromUnixTimeSeconds(timestamp.Value);
            var tolerance = TimeSpan.FromSeconds(
                Math.Clamp(_options.WebhookToleranceSeconds, 1, 300));

            if ((nowUtc - signedAt).Duration() > tolerance)
            {
                return false;
            }

            var prefix = Encoding.UTF8.GetBytes($"{timestamp.Value}:");
            var payload = new byte[prefix.Length + rawBody.Length];
            Buffer.BlockCopy(prefix, 0, payload, 0, prefix.Length);
            Buffer.BlockCopy(rawBody, 0, payload, prefix.Length, rawBody.Length);

            using var hmac = new HMACSHA256(
                Encoding.UTF8.GetBytes(_options.WebhookSecret));
            var expected = hmac.ComputeHash(payload);

            foreach (var signature in signatures)
            {
                byte[] received;
                try
                {
                    received = Convert.FromHexString(signature);
                }
                catch (FormatException)
                {
                    continue;
                }

                if (received.Length == expected.Length &&
                    CryptographicOperations.FixedTimeEquals(
                        received,
                        expected))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
