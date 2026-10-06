using ChamadosTi.Models.Interfaces;

namespace ChamadosTi.Models
{
    public class Feriado: ISoftDelete
    {
        public int Id { get; set; }
        public string Nome { get; set; } = null!;
        public DateOnly Dia { get; set; }
        public bool Ativo { get; set; }
    }
}