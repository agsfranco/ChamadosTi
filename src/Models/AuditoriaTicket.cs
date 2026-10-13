using System.Text.Json.Serialization;
using ChamadosTi.Models.Enums;
using ChamadosTi.Models.Interfaces;

namespace ChamadosTi.Models
{
    public class AuditoriaTicket: ISoftDelete
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string Log { get; set; } = null!;
        public DateTime RegistradoEm { get; set; } = System.DateTime.UtcNow;
        public bool Ativo { get; set; } = true;
        public virtual Ticket Ticket { get; set; } = null!;
    }

    public class LogAuditoriaTicket
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TicketAcaoLog Acao { get; set; } = TicketAcaoLog.Log_Indefinido;
        public string Usuario { get; set; } = string.Empty;
        public Dictionary<string, string>? CamposModificados { get; set; }
        public string? Mensagem { get; set; }
    }
}