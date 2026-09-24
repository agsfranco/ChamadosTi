using ChamadosTi.Models.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace ChamadosTi.Models
{
    public class Usuario : IdentityUser<int>, ISoftDelete
    {
        public int SetorId { get; set; }        
        public int? DadoTecnicoId { get; set; }
        public bool EhTecnico { get; set; }
        public string Nome { get; set; } = null!;
        public bool Ativo { get; set; } = true;
        public virtual Setor Setores { get; set; } = null!;
        public virtual DadoTecnico? DadosTecnicos { get; set; }
    }
}