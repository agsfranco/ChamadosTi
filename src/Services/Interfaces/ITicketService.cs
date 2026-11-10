using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using ChamadosTi.ViewModels.Ticket;

namespace ChamadosTi.Services.Interfaces
{
    public interface ITicketService
    {
        // Consultas
        Task<List<Ticket>> ListarParaUsuarioAsync(int usuarioId, bool ehTecnico, bool verTodos);
        Task<Ticket?> ObterPorIdAsync(int id);
        Task<Ticket?> ObterComStatusAsync(int id);
        Task<Ticket?> ObterDetalhesAsync(int id);
        Task<List<Ticket>> ObterChamadosAnterioresAsync(int ticketId, List<int> itensIds, int limite);
        Task<List<Ticket>> ObterChamadosPorItemAsync(int itemId);
        Task<Status?> ObterStatusPorCodigoAsync(int codigo);
        Task<List<AuditoriaTicket>> ObterAuditoriasAsync(int ticketId);
        Task<AuditoriaTicket?> ObterUltimaAuditoriaPausaAsync(int ticketId);
        Task<TicketIndicadoresDTO> ContarIndicadoresAsync(int? usuarioId, DateTime agoraUtc);

        // SLA
        Task<(DateTime PrazoResposta, DateTime PrazoResolucao)> CalcularPrazosSlaAsync(DateTime dataAberturaUtc, int? setorUsuarioId, int tempoRespostaPadrao, int tempoResolucaoPadrao);

        // Comandos
        Task CriarAsync(Ticket ticket, IEnumerable<int>? itensIds, string usuarioNome, Dictionary<string, string> campos);
        Task RegistrarTransicaoAsync(Ticket ticket, TicketAcaoLog acao, string usuarioNome, string? mensagem, Dictionary<string, string>? campos);
        Task RegistrarAuditoriaAsync(int ticketId, TicketAcaoLog acao, string usuarioNome, string? mensagem, Dictionary<string, string>? campos);
        Task AlterarItensAsync(int ticketId, IEnumerable<int>? itensIds, string usuarioNome, Dictionary<string, string> campos);
    }
}
