using ChamadosTi.Models.Interfaces;

namespace ChamadosTi.Models
{
    public class Categoria: ISoftDelete, INomeado
    {
        public int Id { get; set; }
        public int? EspecialidadeId { get; set; }
        public string Nome { get; set; } = null!;
        public string? Descricao { get; set; }
        public bool Ativo { get; set; } = true;
        public virtual Especialidade? Especialidades { get; set; } 
    }
}