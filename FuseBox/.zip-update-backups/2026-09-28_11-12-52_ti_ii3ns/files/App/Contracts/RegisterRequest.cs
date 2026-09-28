using System.ComponentModel.DataAnnotations;

namespace FuseBox.App.Contracts
{
    public sealed class RegisterRequest
    {
        [Required]
        [EmailAddress]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(128, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;
    }
}
