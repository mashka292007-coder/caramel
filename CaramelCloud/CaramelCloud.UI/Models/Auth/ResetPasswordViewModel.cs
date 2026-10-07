using System.ComponentModel.DataAnnotations;

namespace CaramelCloud.UI.Models.Auth
{
    public class ResetPasswordViewModel
    {
        [Required] public string Token { get; set; } = string.Empty;
        [Required] public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обов'язковий")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Від 6 до 100 символів")]
        [DataType(DataType.Password)]
        [Display(Name = "Новий пароль")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Паролі не співпадають")]
        [Display(Name = "Підтвердіть пароль")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
