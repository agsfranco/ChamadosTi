using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using ChamadosTi.Services;
using ChamadosTi.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChamadosTi.Tests.Services
{
    public class TicketServiceTests : IDisposable
    {
        private readonly SqliteConnection _conexao;
        private readonly AppDbContext _ctx;
        private readonly DadosDeTeste _dados;
        private readonly TicketService _servico;

        public TicketServiceTests()
        {
            _conexao = new SqliteConnection("DataSource=:memory:");
            _conexao.Open();
            _ctx = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conexao).Options);
            _ctx.Database.EnsureCreated();
            _dados = SeedData.Popular(_ctx);
            _ctx.ChangeTracker.Clear();

            _servico = new TicketService(
                _ctx,
                TimeProvider.System,
                new FeriadoService(_ctx),
                new HorarioService(_ctx, new SetorService(_ctx)));
        }

        [Fact]
        public async Task CriarAsync_ItemInexistente_NaoPersisteOChamado()
        {
            var statusAberto = _ctx.Status.Single(s => s.Codigo == (int)TicketStatus.Aberto);
            var ticket = new Ticket
            {
                UsuarioId = _dados.Solicitante.Id,
                SetorId = _dados.SetorComum.Id,
                CategoriaId = _dados.Categoria.Id,
                PrioridadeId = _dados.PrioridadePadrao.Id,
                StatusId = statusAberto.Id,
                DataAbertura = new DateTime(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc),
                PrazoRespostaSLA = DateTime.UtcNow,
                PrazoResolucaoSLA = DateTime.UtcNow,
                Titulo = "Teste",
                Comentario = "Teste",
                Ativo = true
            };
            var chamadosAntes = await _ctx.Tickets.IgnoreQueryFilters().CountAsync();

            // O item 99999 não existe: a gravação dos itens deve falhar e desfazer o chamado.
            await Assert.ThrowsAnyAsync<Exception>(() =>
                _servico.CriarAsync(ticket, new[] { 99999 }, "Teste", new Dictionary<string, string>()));

            _ctx.ChangeTracker.Clear();
            Assert.Equal(chamadosAntes, await _ctx.Tickets.IgnoreQueryFilters().CountAsync());
            Assert.Equal(0, await _ctx.AuditoriasTickets.CountAsync());
        }

        public void Dispose()
        {
            _ctx.Dispose();
            _conexao.Dispose();
        }
    }
}
