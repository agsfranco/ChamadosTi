using ChamadosTi.Models.Interfaces;

namespace ChamadosTi.Models
{
    public class Ticket: ISoftDelete
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int? UsuarioTecnicoId { get; set; }
        public int SetorId { get; set; }
        public int CategoriaId { get; set; }
        public int PrioridadeId { get; set; }
        public int StatusId { get; set; }
        public int? TicketOrigemId { get; set; }
        public DateTime DataAbertura { get; set; } = System.DateTime.UtcNow;
        public DateTime? DataFechamento { get; set; }
        public DateTime PrazoRespostaSLA { get; set; }
        public DateTime PrazoResolucaoSLA { get; set; }
        public string Titulo { get; set; } = null!;
        public string Comentario  { get; set; } = null!;
        public bool Ativo { get; set; } = true;
        public virtual Usuario Usuarios { get; set; } = null!;
        public virtual Usuario? UsuariosTecnicos { get; set; }
        public virtual Setor Setores { get; set; } = null!;
        public virtual Categoria Categorias { get; set; } = null!;
        public virtual Prioridade Prioridades { get; set; } = null!;
        public virtual Status Status { get; set; } = null!;
        public virtual Ticket? TicketOrigem { get; set; }
        public virtual ICollection<ItemTicket>? ItensTickets { get; set; } = new List<ItemTicket>();
        public virtual ICollection<AuditoriaTicket>? AuditoriaTickets { get; set; } = new List<AuditoriaTicket>();
    }
}