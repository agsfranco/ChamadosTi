using ChamadosTi.Models.Interfaces;

namespace ChamadosTi.Models
{
    public class Item: ISoftDelete 
    {
        public int Id { get; set; }
        public int TipoId { get; set; }
        public int MarcaId { get; set; }
        public int? SetorId { get; set; }
        public string Nome { get; set; } = null!;
        public string? Comentario { get; set; }
        public DateTime CriadoEm { get; set; } = System.DateTime.UtcNow;
        public DateTime ExcluidoEm { get; set; }
        public bool Ativo { get; set; } = true;
        public virtual Tipo Tipos { get; set; } = null!;
        public virtual Marca Marcas { get; set; } = null!;
        public virtual Setor? Setores { get; set; } = null!;
        public virtual ICollection<ItemTicket>? ItensTickets { get; set; } = new List<ItemTicket>();
    }
}