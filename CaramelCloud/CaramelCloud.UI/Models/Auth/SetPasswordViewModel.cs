using System.ComponentModel.DataAnnotations;

namespace CaramelCloud.UI.Models.Auth
{
    public class SetPasswordViewModel
    {
        [Required(ErrorMessage = "Пароль обов'язковий")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Від 6 до 100 символів")]
        [DataType(DataType.Password)]
        [Display(Name = "Новий пароль")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Підтвердження обов'язкове")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Паролі не співпадають")]
        [Display(Name = "Підтвердіть пароль")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
