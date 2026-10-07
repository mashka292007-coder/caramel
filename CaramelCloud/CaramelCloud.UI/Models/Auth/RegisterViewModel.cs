using System.ComponentModel.DataAnnotations;

namespace CaramelCloud.UI.Models.Auth
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Ім'я обов'язкове")]
        [StringLength(50)]
        [Display(Name = "Ім'я")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Прізвище обов'язкове")]
        [StringLength(50)]
        [Display(Name = "Прізвище")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email обов'язковий")]
        [EmailAddress(ErrorMessage = "Невірний формат Email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Телефон обов'язковий")]
        [RegularExpression(@"^\+380\d{9}$", ErrorMessage = "Формат: +380XXXXXXXXX (9 цифр після +380)")]
        [Display(Name = "Телефон")]
        public string Phone { get; set; } = string.Empty;
        [Required(ErrorMessage = "Пароль обов'язковий")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль від 6 до 100 символів")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Підтвердження паролю обов'язкове")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Паролі не співпадають")]
        [Display(Name = "Підтвердіть пароль")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
