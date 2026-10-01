using System.ComponentModel.DataAnnotations;

namespace ChamadosTi.ViewModels.Marca
{
    public class MarcaViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "CampoObrigatorio")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "MinMax")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        public bool Ativo { get; set; } = true;
    }
}
