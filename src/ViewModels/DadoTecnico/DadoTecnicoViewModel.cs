using System.ComponentModel.DataAnnotations;

namespace ChamadosTi.ViewModels.DadoTecnico
{
    public class DadoTecnicoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "CampoObrigatorio")]
        [Display(Name = "Especialidade")]
        public int? EspecialidadeId { get; set; }

        [Required(ErrorMessage = "CampoObrigatorio")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "MinMax")]
        [Display(Name = "Nivel")]
        public string Nivel { get; set; } = string.Empty;

        public bool Ativo { get; set; } = true;
    }
}
