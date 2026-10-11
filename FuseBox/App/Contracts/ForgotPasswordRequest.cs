using System.ComponentModel.DataAnnotations;

namespace FuseBox.App.Contracts.Auth
{
    public sealed class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;
    }
}
