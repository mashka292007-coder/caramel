using System.ComponentModel.DataAnnotations;

namespace CaramelCloud.UI.Models.Auth
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Ім'я обов'язкове")]
        [StringLength(50)]
        [Display(Name = "Ім'я")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Прізвище обов'язкове")]
        [StringLength(50)]
        [Display(Name = "Прізвище")]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Невірний формат телефону")]
        [Display(Name = "Телефон")]
        public string? Phone { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "День народження")]
        public DateTime? BirthDate { get; set; }

        [EmailAddress(ErrorMessage = "Невірний формат email")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        public bool IsGoogleUser { get; set; }
        public bool HasPassword { get; set; }

        public bool HasFacebookEmail => Email != null && Email.EndsWith("@caramelcloud.local");
    }
}
