using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ChamadosTi.ViewModels.Horario
{
    public class HorarioViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "CampoObrigatorio")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "MinMax")]
        [Display(Name = "DiaDaSemana")]
        public string DiaDaSemana { get; set; } = string.Empty;

        [Required(ErrorMessage = "CampoObrigatorio")]
        [DataType(DataType.Time)]
        [Display(Name = "JornadaInicio")]
        public TimeOnly? JornadaInicio { get; set; }

        [Required(ErrorMessage = "CampoObrigatorio")]
        [DataType(DataType.Time)]
        [Display(Name = "JornadaFim")]
        public TimeOnly? JornadaFim { get; set; }

        [DataType(DataType.Time)]
        [Display(Name = "AlmocoInicio")]
        public TimeOnly? AlmocoInicio { get; set; }

        [DataType(DataType.Time)]
        [Display(Name = "AlmocoFim")]
        public TimeOnly? AlmocoFim { get; set; }

        public bool Ativo { get; set; } = true;

        // Lista de IDs dos setores vinculados a este turno específico
        [Display(Name = "SetoresVinculados")]
        public List<int> SetoresIdsSelecionados { get; set; } = new List<int>();
    }
}
