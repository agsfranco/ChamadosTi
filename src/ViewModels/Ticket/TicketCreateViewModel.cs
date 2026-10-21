using System.ComponentModel.DataAnnotations;

namespace ChamadosTi.ViewModels.Ticket
{
    public class TicketCreateViewModel
    {
        [Required(ErrorMessage = "CampoObrigatorio")]
        [StringLength(150, MinimumLength = 5, ErrorMessage = "MinMax")]
        [Display(Name = "Titulo")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "CampoObrigatorio")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "MinMax")]
        [Display(Name = "Comentario")]
        public string Comentario { get; set; } = string.Empty;

        [Required(ErrorMessage = "CampoObrigatorio")]
        [Display(Name = "Categoria")]
        public int? CategoriaId { get; set; }

        // Lista de IDs dos itens de hardware/software selecionados pelo usuário na abertura
        [Display(Name = "ItensAfetados")]
        public List<int> ItensIdsSelecionados { get; set; } = new List<int>();
    }
}
