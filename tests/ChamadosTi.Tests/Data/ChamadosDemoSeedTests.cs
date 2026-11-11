using System.Text.Json;
using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ChamadosTi.Tests.Data
{
    public class ChamadosDemoSeedTests : IDisposable
    {
        private readonly SqliteConnection _conexao;
        private readonly AppDbContext _ctx;
        private readonly Mock<UserManager<IdentityUser>> _userManager;

        public ChamadosDemoSeedTests()
        {
            _conexao = new SqliteConnection("DataSource=:memory:");
            _conexao.Open();
            _ctx = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conexao).Options);
            _ctx.Database.EnsureCreated();

            // Simula o UserManager gravando as contas na mesma base (o FK de Usuarios exige a identidade).
            _userManager = new Mock<UserManager<IdentityUser>>(
                Mock.Of<IUserStore<IdentityUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
            _userManager.Setup(u => u.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
            _userManager
                .Setup(u => u.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
                .Callback<IdentityUser, string>((usuario, _) => { _ctx.Users.Add(usuario); _ctx.SaveChanges(); })
                .ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(u => u.AddToRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        }

        [Fact]
        public async Task CargaDemo_ChamadosTemHistoricoCoerenteComSeuEstado()
        {
            await CatalogDataSeeder.SeedDemonstracaoAsync(_ctx, _userManager.Object);
            _ctx.ChangeTracker.Clear();

            var chamados = await _ctx.Tickets
                .Include(t => t.AuditoriaTickets)
                .Include(t => t.ItensTickets)
                .Include(t => t.Status)
                .ToListAsync();

            Assert.True(chamados.Count >= 30, $"Esperados ao menos 30 chamados, gerados {chamados.Count}.");
            var agora = DateTime.UtcNow;
            var tecnicos = await _ctx.Usuarios.Where(u => u.DadoTecnicoId != null).ToDictionaryAsync(u => u.Id, u => u.Nome);
            var setorDoSolicitante = await _ctx.Usuarios.ToDictionaryAsync(u => u.Id, u => u.SetorId);

            var cenarios = new HashSet<TicketStatus>();

            foreach (var chamado in chamados)
            {
                var logs = chamado.AuditoriaTickets!.OrderBy(a => a.RegistradoEm).ToList();
                var decodificados = logs.Select(a => JsonSerializer.Deserialize<LogAuditoriaTicket>(a.Log)!).ToList();
                var acoes = decodificados.Select(l => l.Acao).ToList();

                // Primeiro evento é a abertura, e nenhum evento acontece antes ou depois do intervalo válido.
                Assert.Equal(TicketAcaoLog.Log_AberturaChamado, acoes[0]);
                Assert.Equal(chamado.DataAbertura, logs[0].RegistradoEm);
                Assert.All(logs, a => Assert.True(a.RegistradoEm <= agora, "Evento registrado no futuro."));
                Assert.True(logs.Zip(logs.Skip(1)).All(p => p.First.RegistradoEm <= p.Second.RegistradoEm), "Eventos fora de ordem.");

                // O estado do chamado precisa corresponder à última transição registrada.
                var estado = EstadoPelasAcoes(acoes);
                cenarios.Add(estado);
                Assert.Equal((int)estado, _ctx.Status.Single(s => s.Id == chamado.StatusId).Codigo);

                var assumiu = acoes.Contains(TicketAcaoLog.Log_ChamadoAssumido);
                Assert.Equal(assumiu, chamado.UsuarioTecnicoId != null);
                if (assumiu)
                {
                    var assumido = decodificados.First(l => l.Acao == TicketAcaoLog.Log_ChamadoAssumido);
                    Assert.Equal(tecnicos[chamado.UsuarioTecnicoId!.Value], assumido.Usuario);
                    Assert.Equal(assumido.Usuario, assumido.CamposModificados![TicketAcaoLog.Log_TecnicoAtribuido.ToString()]);
                }

                var fechado = estado is TicketStatus.Resolvido or TicketStatus.Cancelado;
                Assert.Equal(fechado, chamado.DataFechamento != null);
                if (fechado)
                {
                    Assert.True(chamado.DataFechamento >= chamado.DataAbertura);
                    Assert.Equal(chamado.DataFechamento, logs.Last().RegistradoEm);
                }

                // Prazos: resposta antes da resolução, e a extensão de prazo do log (quando há pausa) bate com o chamado.
                Assert.True(chamado.PrazoRespostaSLA <= chamado.PrazoResolucaoSLA);
                var extensao = decodificados
                    .Where(l => l.CamposModificados != null && l.CamposModificados.ContainsKey(TicketAcaoLog.Log_PrazoResolucaoEstendido.ToString()))
                    .ToList();
                if (extensao.Count > 0)
                {
                    var ultimo = extensao.Last().CamposModificados![TicketAcaoLog.Log_PrazoResolucaoEstendido.ToString()];
                    Assert.Equal(chamado.PrazoResolucaoSLA, DateTime.Parse(ultimo, null, System.Globalization.DateTimeStyles.RoundtripKind));
                }

                // O chamado pertence a um solicitante de outro setor que não o de TI.
                Assert.Equal(setorDoSolicitante[chamado.UsuarioId], chamado.SetorId);
            }

            // A carga precisa cobrir os principais estados, para a tela de detalhe ter exemplos de cada um.
            Assert.Contains(TicketStatus.Resolvido, cenarios);
            Assert.Contains(TicketStatus.EmAtendimento, cenarios);
            Assert.Contains(TicketStatus.Cancelado, cenarios);
            Assert.Contains(TicketStatus.Aberto, cenarios);
        }

        [Fact]
        public async Task CargaDemo_ExecutarDeNovo_NaoDuplicaChamados()
        {
            await CatalogDataSeeder.SeedDemonstracaoAsync(_ctx, _userManager.Object);
            var quantidade = await _ctx.Tickets.CountAsync();

            await CatalogDataSeeder.SeedDemonstracaoAsync(_ctx, _userManager.Object);

            Assert.Equal(quantidade, await _ctx.Tickets.CountAsync());
        }

        // Mesma lógica de transição do TicketsController: a última ação de estado define o status.
        private static TicketStatus EstadoPelasAcoes(List<TicketAcaoLog> acoes)
        {
            var ultimaTransicao = acoes.LastOrDefault(a => a is TicketAcaoLog.Log_ChamadoAssumido or TicketAcaoLog.Log_ChamadoPendente
                                                          or TicketAcaoLog.Log_ChamadoRetomado or TicketAcaoLog.Log_ChamadoResolvido
                                                          or TicketAcaoLog.Log_ChamadoCancelado);
            return ultimaTransicao switch
            {
                TicketAcaoLog.Log_ChamadoAssumido or TicketAcaoLog.Log_ChamadoRetomado => TicketStatus.EmAtendimento,
                TicketAcaoLog.Log_ChamadoPendente => TicketStatus.Pendente,
                TicketAcaoLog.Log_ChamadoResolvido => TicketStatus.Resolvido,
                TicketAcaoLog.Log_ChamadoCancelado => TicketStatus.Cancelado,
                _ => TicketStatus.Aberto,
            };
        }

        public void Dispose()
        {
            _ctx.Dispose();
            _conexao.Dispose();
        }
    }
}
