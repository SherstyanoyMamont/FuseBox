using System.ComponentModel.DataAnnotations;

namespace FuseBox.App.Contracts
{
    public sealed class ResetPasswordRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [StringLength(128, MinimumLength = 8)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
