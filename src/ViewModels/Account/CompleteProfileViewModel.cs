using System.ComponentModel.DataAnnotations;
using ChamadosTi.ViewModels.DadoTecnico;

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

        public bool EhTecnico { get; set; }

        [Display(Name = "DadoTecnico")]
        public int? DadoTecnicoId { get; set; }
        // Opções dos dropdowns (preenchidas pela controller)
        public IEnumerable<ChamadosTi.Models.Setor> Setores { get; set; } = new List<ChamadosTi.Models.Setor>();

        public IEnumerable<DadoTecnicoOpcaoDTO> DadosTecnicos { get; set; } = new List<DadoTecnicoOpcaoDTO>();
    }
}
