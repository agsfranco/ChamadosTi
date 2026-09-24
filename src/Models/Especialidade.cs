using ChamadosTi.Models.Interfaces;

namespace ChamadosTi.Models
{
    public class Especialidade: ISoftDelete
    {
        public int Id { get; set; }
        public string Nome { get; set; } = null!;
        public string? Descricao { get; set; }  
        public bool Ativo { get; set; } = true;
    }
}