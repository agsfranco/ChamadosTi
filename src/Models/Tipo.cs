using ChamadosTi.Models.Interfaces;

namespace ChamadosTi.Models
{
    public class Tipo: ISoftDelete
    {
        public int Id { get; set; }
        public string Nome { get; set; } = null!;
        public string? Comentario { get; set; }
        public bool Ativo { get; set; } = true;
    }
}