using System.ComponentModel.DataAnnotations;
using ChamadosTi.Models.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace ChamadosTi.Models
{
    public class Usuario : IdentityUser<int>, ISoftDelete
    {
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        public int SetorId { get; set; }        
        public int? DadoTecnicoId { get; set; }
        public bool EhTecnico { get; set; }
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O campo {0} precisa ter entre {2} e {1} caracteres")]
        public string Nome { get; set; } = null!;
        public bool Ativo { get; set; } = true;
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        public virtual Setor Setores { get; set; } = null!;
        public virtual DadoTecnico? DadosTecnicos { get; set; }
    }
}