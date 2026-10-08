using ChamadosTi.Models.Interfaces;

namespace ChamadosTi.Models
{
    public class Setor: ISoftDelete
    {
        public int Id { get; set; }
        public string Nome { get; set; } = null!;
        public string? Descricao { get; set; }
        public bool Ti { get; set; }
        public bool Ativo { get; set; } = true;
        public virtual ICollection<SetorHorario>? SetoresHorarios { get; set; } = new List<SetorHorario>();
    }
}