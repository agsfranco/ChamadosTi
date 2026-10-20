using ChamadosTi.Models.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace ChamadosTi.Models
{
    public class Usuario: ISoftDelete
    {
        public int Id { get; set; }
        public string AspNetUserId { get; set; } = null!;
        public int? SetorId { get; set; }        
        public int? DadoTecnicoId { get; set; }
        public string Nome { get; set; } = null!;
        public virtual Setor? Setores { get; set; } = null!;
        public virtual DadoTecnico? DadosTecnicos { get; set; }
        public virtual IdentityUser AspNetUser { get; set; } = null!;
        public bool Ativo { get; set; } = true;        
    }
}