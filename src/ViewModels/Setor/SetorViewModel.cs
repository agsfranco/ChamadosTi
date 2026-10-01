using System.ComponentModel.DataAnnotations;

namespace ChamadosTi.ViewModels.Setor
{
    public class SetorViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "CampoObrigatorio")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "MinMax")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "MinMax")]
        [Display(Name = "Descricao")]
        public string? Descricao { get; set; }

        public bool Ativo { get; set; } = true;
    }
}
