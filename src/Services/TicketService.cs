using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using ChamadosTi.Services.Interfaces;
using ChamadosTi.Utils;
using ChamadosTi.ViewModels.Ticket;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ChamadosTi.Services
{
    public class TicketService : ITicketService
    {
        private readonly AppDbContext _context;
        private readonly TimeProvider _timeProvider;
        private readonly IFeriadoService _feriadoService;
        private readonly IHorarioService _horarioService;

        public TicketService(AppDbContext context, TimeProvider timeProvider, IFeriadoService feriadoService, IHorarioService horarioService)
        {
            _context = context;
            _timeProvider = timeProvider;
            _feriadoService = feriadoService;
            _horarioService = horarioService;
        }

        public async Task<List<Ticket>> ListarParaUsuarioAsync(int usuarioId, bool ehTecnico, bool verTodos)
        {
            var query = _context.Tickets
                                .Include(t => t.Status)
                                .Include(t => t.Prioridades)
                                .Include(t => t.Categorias)
                                .Include(t => t.Usuarios)
                                .Include(t => t.UsuariosTecnicos)
                                .AsQueryable();

            if (!verTodos)
            {
                query = ehTecnico
                    ? query.Where(t => t.UsuarioId == usuarioId || t.UsuarioTecnicoId == usuarioId || t.Status.Codigo == (int)TicketStatus.Aberto)
                    : query.Where(t => t.UsuarioId == usuarioId);
            }

            return await query.OrderByDescending(t => t.DataAbertura).ToListAsync();
        }

        public async Task<Ticket?> ObterPorIdAsync(int id)
        {
            return await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Ticket?> ObterComStatusAsync(int id)
        {
            return await _context.Tickets.Include(t => t.Status).FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Ticket?> ObterDetalhesAsync(int id)
        {
            return await _context.Tickets
                                 .Include(t => t.Status)
                                 .Include(t => t.Prioridades)
                                 .Include(t => t.Categorias)
                                 .Include(t => t.Usuarios)
                                 .Include(t => t.UsuariosTecnicos)
                                 .Include(t => t.Setores)
                                 .Include(t => t.ItensTickets!)
                                 .ThenInclude(it => it.Item)
                                 .ThenInclude(i => i.Marcas)
                                 .Include(t => t.ItensTickets!)
                                 .ThenInclude(it => it.Item)
                                 .ThenInclude(i => i.Tipos)
                                 .FirstOrDefaultAsync(t => t.Id == id);
        }

        // Chamados mais recentes, de outros tickets, que compartilham ao menos um dos itens.
        public async Task<List<Ticket>> ObterChamadosAnterioresAsync(int ticketId, List<int> itensIds, int limite)
        {
            return await _context.Tickets
                                 .Include(t => t.Status)
                                 .Include(t => t.ItensTickets)
                                 .Include(t => t.AuditoriaTickets)
                                 .Where(t => t.Id != ticketId && t.ItensTickets!.Any(it => itensIds.Contains(it.ItemId)))
                                 .OrderByDescending(t => t.DataAbertura)
                                 .Take(limite)
                                 .ToListAsync();
        }

        public async Task<List<Ticket>> ObterChamadosPorItemAsync(int itemId)
        {
            return await _context.Tickets
                                 .Include(t => t.Status)
                                 .Include(t => t.AuditoriaTickets)
                                 .Where(t => t.ItensTickets!.Any(it => it.ItemId == itemId))
                                 .OrderByDescending(t => t.DataAbertura)
                                 .ToListAsync();
        }

        public async Task<Status?> ObterStatusPorCodigoAsync(int codigo)
        {
            return await _context.Status.FirstOrDefaultAsync(s => s.Codigo == codigo);
        }

        public async Task<List<AuditoriaTicket>> ObterAuditoriasAsync(int ticketId)
        {
            return await _context.AuditoriasTickets
                                 .Where(a => a.TicketId == ticketId)
                                 .OrderBy(a => a.RegistradoEm)
                                 .ToListAsync();
        }

        public async Task<AuditoriaTicket?> ObterUltimaAuditoriaPausaAsync(int ticketId)
        {
            return await _context.AuditoriasTickets
                                 .Where(a => a.TicketId == ticketId && a.Log.Contains(TicketAcaoLog.Log_ChamadoPendente.ToString()))
                                 .OrderByDescending(a => a.RegistradoEm)
                                 .FirstOrDefaultAsync();
        }

        public async Task<TicketIndicadoresDTO> ContarIndicadoresAsync(int? usuarioId, DateTime agoraUtc)
        {
            var queryBase = _context.Tickets.Where(t => t.Ativo == true);

            if (usuarioId.HasValue)
            {
                queryBase = queryBase.Where(t => t.UsuarioId == usuarioId.Value);
            }

            return new TicketIndicadoresDTO
            {
                TotalNovosAbertos = await queryBase.CountAsync(t => t.Status.Codigo == (int)TicketStatus.Aberto),
                TotalEmAtendimento = await queryBase.CountAsync(t => t.Status.Codigo == (int)TicketStatus.EmAtendimento),
                TotalPendentes = await queryBase.CountAsync(t => t.Status.Codigo == (int)TicketStatus.Pendente),
                TotalSlaVencidos = await queryBase.CountAsync(t => t.Status.Codigo < (int)TicketStatus.Resolvido && t.PrazoResolucaoSLA < agoraUtc)
            };
        }

        public async Task<(DateTime PrazoResposta, DateTime PrazoResolucao)> CalcularPrazosSlaAsync(DateTime dataAberturaUtc, int? setorUsuarioId, int tempoRespostaPadrao, int tempoResolucaoPadrao)
        {
            var feriados = await _feriadoService.ObterAPartirDeAsync(DateOnly.FromDateTime(dataAberturaUtc));
            var horarios = await _horarioService.ObterHorariosSlaAsync(setorUsuarioId);

            var prazoResposta = SlaCalculator.CalcularPrazoSlaReal(feriados, horarios, dataAberturaUtc, tempoRespostaPadrao);
            var prazoResolucao = SlaCalculator.CalcularPrazoSlaReal(feriados, horarios, dataAberturaUtc, tempoResolucaoPadrao);

            return (prazoResposta, prazoResolucao);
        }

        // Abertura: grava o chamado, depois os itens e a auditoria (dois SaveChanges, como antes).
        public async Task CriarAsync(Ticket ticket, IEnumerable<int>? itensIds, string usuarioNome, Dictionary<string, string> campos)
        {
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            if (itensIds != null && itensIds.Any())
            {
                foreach (var itemId in itensIds)
                {
                    _context.ItensTickets.Add(new ItemTicket
                    {
                        TicketId = ticket.Id,
                        ItemId = itemId,
                        Ativo = true
                    });
                }
            }

            AdicionarAuditoria(TicketAcaoLog.Log_AberturaChamado, usuarioNome, ticket.Id, null, campos);
            await _context.SaveChangesAsync();
        }

        // Mudança de estado: atualiza o chamado e registra a auditoria em um único SaveChanges.
        public async Task RegistrarTransicaoAsync(Ticket ticket, TicketAcaoLog acao, string usuarioNome, string? mensagem, Dictionary<string, string>? campos)
        {
            _context.Tickets.Update(ticket);
            AdicionarAuditoria(acao, usuarioNome, ticket.Id, mensagem, campos);
            await _context.SaveChangesAsync();
        }

        public async Task RegistrarAuditoriaAsync(int ticketId, TicketAcaoLog acao, string usuarioNome, string? mensagem, Dictionary<string, string>? campos)
        {
            AdicionarAuditoria(acao, usuarioNome, ticketId, mensagem, campos);
            await _context.SaveChangesAsync();
        }

        public async Task AlterarItensAsync(int ticketId, IEnumerable<int>? itensIds, string usuarioNome, Dictionary<string, string> campos)
        {
            await _context.ItensTickets
                          .Where(it => it.TicketId == ticketId)
                          .ExecuteDeleteAsync();

            if (itensIds != null && itensIds.Any())
            {
                foreach (var itemId in itensIds)
                {
                    _context.ItensTickets.Add(new ItemTicket
                    {
                        TicketId = ticketId,
                        ItemId = itemId,
                        Ativo = true
                    });
                }
            }

            AdicionarAuditoria(TicketAcaoLog.Log_ItensAlterados, usuarioNome, ticketId, null, campos);
            await _context.SaveChangesAsync();
        }

        private void AdicionarAuditoria(TicketAcaoLog acao, string usuarioNome, int ticketId, string? mensagem, Dictionary<string, string>? campos)
        {
            var dadosLog = new LogAuditoriaTicket
            {
                Acao = acao,
                Usuario = usuarioNome,
                CamposModificados = campos,
                Mensagem = mensagem
            };

            _context.AuditoriasTickets.Add(new AuditoriaTicket
            {
                TicketId = ticketId,
                Log = JsonSerializer.Serialize(dadosLog),
                RegistradoEm = _timeProvider.GetUtcNow().UtcDateTime,
                Ativo = true
            });
        }
    }
}
