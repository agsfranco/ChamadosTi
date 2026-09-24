using ChamadosTi.Models.Interfaces;

namespace ChamadosTi.Models
{
    public class Status: ISoftDelete
    {
        public int Id { get; set; }
        public int Codigo { get; set; }
        public string Nome { get; set; } = null!;
        public bool Ativo { get; set; } = true;
    }
}