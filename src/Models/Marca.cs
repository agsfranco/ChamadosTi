using ChamadosTi.Models.Interfaces;

namespace ChamadosTi.Models
{
    public class Marca: ISoftDelete, INomeado
    {
        public int Id { get; set; }
        public string Nome { get; set; } = null!;
        public bool Ativo { get; set; } = true;
    }
}