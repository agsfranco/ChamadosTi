using System.ComponentModel.DataAnnotations;

namespace ChamadosTi.ViewModels.Tipo
{
    public class TipoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "CampoObrigatorio")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "MinMax")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "MinMax")]
        [Display(Name = "Comentario")]
        public string? Comentario { get; set; }

        public bool Ativo { get; set; } = true;
    }
}
