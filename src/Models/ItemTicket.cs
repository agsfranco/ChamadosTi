using ChamadosTi.Models.Interfaces;

namespace ChamadosTi.Models
{
    public class ItemTicket: ISoftDelete
    {
        public int ItemId { get; set; }
        public int TicketId { get; set; }
        public string? Comentario { get; set; }
        public bool Ativo { get; set; }        
        public virtual Item Item { get; set; } = null!;
        public virtual Ticket Ticket { get; set; } = null!;
    }
}