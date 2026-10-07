using System.ComponentModel.DataAnnotations;

namespace CaramelCloud.UI.Models.Auth
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email обов'язковий")]
        [EmailAddress(ErrorMessage = "Невірний формат Email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }
}
