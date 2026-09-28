using System.ComponentModel.DataAnnotations;
using ChamadosTi.Models.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace ChamadosTi.Models
{
    public class Usuario: ISoftDelete
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "CampoObrigatorio")]
        public string AspNetUserId { get; set; } = null!;

        [Required(ErrorMessage = "CampoObrigatorio")]
        public int SetorId { get; set; }        
        
        public int? DadoTecnicoId { get; set; }
        
        public bool EhTecnico { get; set; }
        
        [StringLength(100, MinimumLength = 3, ErrorMessage = "MinMax")]
        [Required(ErrorMessage = "CampoObrigatorio")]
        public string Nome { get; set; } = null!;
        
        [Required(ErrorMessage = "CampoObrigatorio")]
        public virtual Setor Setores { get; set; } = null!;
        
        public virtual DadoTecnico? DadosTecnicos { get; set; }
        public virtual IdentityUser AspNetUser { get; set; } = null!;

        public bool Ativo { get; set; } = true;
    }
}