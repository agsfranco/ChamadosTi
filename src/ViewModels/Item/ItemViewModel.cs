using System.ComponentModel.DataAnnotations;

namespace ChamadosTi.ViewModels.Item
{
    public class ItemViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "CampoObrigatorio")]
        [Display(Name = "Tipo")]
        public int? TipoId { get; set; }

        [Required(ErrorMessage = "CampoObrigatorio")]
        [Display(Name = "Marca")]
        public int? MarcaId { get; set; }

        [Display(Name = "Setor")]
        public int? SetorId { get; set; }

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
