using ChamadosTi.Models.Interfaces;

namespace ChamadosTi.Models
{
    public class Prioridade: ISoftDelete
    {
        public int Id { get; set; }
        public string Nome { get; set; } = null!;
        public int TempoRespostaPadrao { get; set; }
        public int TempoResolucaoPadrao { get; set; }
        public bool Padrao { get; set; }
        public bool Ativo { get; set; } = true;
    }

}