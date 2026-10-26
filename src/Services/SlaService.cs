using ChamadosTi.Data;
using ChamadosTi.Models.Enums;
using ChamadosTi.Services.Interfaces;
using ChamadosTi.ViewModels.Sla;
using Microsoft.EntityFrameworkCore;

namespace ChamadosTi.Services
{
    public class SlaService : ISlaService
    {
        private readonly AppDbContext _context;

        public SlaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SlaDashboardViewModel> ObterDadosDashboardSlaAsync(DateOnly prazoInicial, DateOnly prazoFinal)
        {
            var agoraUtc = DateTime.UtcNow;
            
            // Converte os limites para DateTime no fuso UTC para bater com as propriedades do banco
            var dataInicioUtc = prazoInicial.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var dataFimUtc = prazoFinal.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

            // 1. Queries de Base FILTRADAS por período (Baseado na Data de Abertura do chamado)
            var todosTickets = await _context.Tickets
                .Include(t => t.Status)
                .Include(t => t.Prioridades)
                .Include(t => t.UsuariosTecnicos)
                .Where(t => t.DataAbertura >= dataInicioUtc && t.DataAbertura <= dataFimUtc)
                .ToListAsync();

            var resolvidoId = (int)TicketStatus.Resolvido;
            var canceladoId = (int)TicketStatus.Cancelado;

            // 2. Contadores dos Cards (O backlog e vencidos agora respeitam o escopo da janela de tempo filtrada)
            var backlog = todosTickets.Count(t => t.StatusId < resolvidoId);
            var vencidos = todosTickets.Count(t => t.StatusId < resolvidoId && t.PrazoResolucaoSLA < agoraUtc);

            // 3. Agregações para Gráficos
            var statusGrafico = todosTickets
                .GroupBy(t => t.Status.Nome)
                .ToDictionary(g => g.Key, g => g.Count());

            var prioridadeGrafico = todosTickets
                .GroupBy(t => t.Prioridades.Nome)
                .ToDictionary(g => g.Key, g => g.Count());

            // 4. Cálculo de Conformidade de SLA (Resolvidos dentro do Prazo)
            var chamadosEncerrados = todosTickets.Where(t => t.StatusId == resolvidoId).ToList();
            double taxaConformidade = 100;

            if (chamadosEncerrados.Any())
            {
                // Simplificação inicial baseada no timestamp de fechamento real guardado no banco
                var dentroDoPrazo = chamadosEncerrados.Count(t => t.DataFechamento <= t.PrazoResolucaoSLA);
                taxaConformidade = Math.Round(((double)dentroDoPrazo / chamadosEncerrados.Count) * 100, 1);
            }

            // 5. Linha do Tempo: Evolução de Aberturas consolidada por Dia Real
            var evolucaoAberturas = todosTickets
                .Where(t => t.DataAbertura >= dataInicioUtc && t.DataAbertura <= dataFimUtc)
                .GroupBy(t => t.DataAbertura.ToLocalTime().Date) // <-- Agrupa pela data pura, sem horas!
                .OrderBy(g => g.Key)
                .ToDictionary(
                    g => g.Key.ToString("dd/MM"), // <-- Transforma em string curta apenas na saída
                    g => g.Count()
                );

            // 6. Performance por Analista Técnico
            var analistasPerformance = todosTickets
                .Where(t => t.UsuarioTecnicoId != null)
                .GroupBy(t => t.UsuariosTecnicos!.Nome)
                .Select(g => new PerformanceAnalistaDTO
                {
                    NomeAnalista = g.Key,
                    ChamadosAssumidos = g.Count(),
                    ChamadosResolvidos = g.Count(t => t.StatusId == resolvidoId),
                    TaxaAcertoSla = g.Count(t => t.StatusId == resolvidoId) > 0
                        ? Math.Round(((double)g.Count(t => t.StatusId == resolvidoId && t.DataFechamento <= t.PrazoResolucaoSLA) / g.Count(t => t.StatusId == resolvidoId)) * 100, 1)
                        : 100
                }).ToList();

            return new SlaDashboardViewModel
            {
                TotalBacklog = backlog,
                TotalSlaVencidos = vencidos,
                TaxaConformidadeSla = taxaConformidade,
                TempoMedioResolucaoFormatado = "2h 45m", // Mock inicial
                ChamadosPorStatus = statusGrafico,
                ChamadosPorPrioridade = prioridadeGrafico,
                EvolucaoAberturasUltimosDias = evolucaoAberturas,
                PerformanceAnalistas = analistasPerformance
            };
        }
    }
}
