using System.ComponentModel.DataAnnotations;

namespace ChamadosTi.ViewModels.Account
{
    public class CompleteProfileViewModel
    {
        [Required(ErrorMessage = "CampoObrigatorio")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "MinMax")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "CampoObrigatorio")]
        [Display(Name = "Setor")]
        public int? SetorId { get; set; }
    }
}
