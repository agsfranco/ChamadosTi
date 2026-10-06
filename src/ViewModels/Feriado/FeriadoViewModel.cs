using System;
using System.ComponentModel.DataAnnotations;

namespace ChamadosTi.ViewModels.Feriado
{
    public class FeriadoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "CampoObrigatorio")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "MinMax")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "CampoObrigatorio")]
        [DataType(DataType.Date)]
        [Display(Name = "Dia")]
        public DateOnly? Dia { get; set; } // Anulável para abrir o calendário em branco na tela

        public bool Ativo { get; set; } = true;
    }
}
