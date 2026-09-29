using System.ComponentModel.DataAnnotations;

namespace ChamadosTi.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "The Email field is required.")]
        [EmailAddress(ErrorMessage = "The Email field is not a valid e-mail address.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }
}
