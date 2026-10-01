using System.ComponentModel.DataAnnotations;

namespace ChamadosTi.ViewModels.Prioridade
{
    public class PrioridadeViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "CampoObrigatorio")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "MinMax")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "CampoObrigatorio")]
        [Range(1, 720, ErrorMessage = "MinMaxValores")] // Limite razoável de 1 hora até 30 dias (720h)
        [Display(Name = "TempoResposta")]
        public int TempoRespostaPadrao { get; set; }

        [Required(ErrorMessage = "CampoObrigatorio")]
        [Range(1, 720, ErrorMessage = "MinMaxValores")]
        [Display(Name = "TempoResolucao")]
        public int TempoResolucaoPadrao { get; set; }

        public bool Ativo { get; set; } = true;
    }
}
