using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using ChamadosTi.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ChamadosTi.Tests.Controllers.TicketsControllerTests
{
    public class IndexTests : IDisposable
    {
        private readonly TicketsControllerFixture _fixture = new();

        public void Dispose() => _fixture.Dispose();

        [Fact]
        public async Task Index_SemTickets_RetornaViewComListaVazia()
        {
            var controller = _fixture.CriarController(_fixture.Dados.Solicitante, ehTecnico: false);

            var result = await controller.Index();

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Ticket>>(view.Model);
            Assert.Empty(model);
        }

        [Fact]
        public async Task Index_RetornaTicketsOrdenadosPorDataAberturaDecrescente()
        {
            var dados = _fixture.Dados;
            var dataBase = new DateTime(2026, 10, 27, 10, 0, 0, DateTimeKind.Utc);

            SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, dataAberturaUtc: dataBase);
            SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, dataAberturaUtc: dataBase.AddDays(2));
            SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, dataAberturaUtc: dataBase.AddDays(1));
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            var result = await controller.Index();

            var model = Assert.IsAssignableFrom<List<Ticket>>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(
                new[] { dataBase.AddDays(2), dataBase.AddDays(1), dataBase },
                model.Select(t => t.DataAbertura));
        }

        [Fact]
        public async Task Index_NaoExibeTicketsInativos()
        {
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto);
            ticket.Ativo = false;
            _fixture.Contexto.SaveChanges();
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            var result = await controller.Index();

            var model = Assert.IsAssignableFrom<List<Ticket>>(Assert.IsType<ViewResult>(result).Model);
            Assert.Empty(model);
        }

        private static List<int> IdsDoModelo(IActionResult result)
            => Assert.IsAssignableFrom<List<Ticket>>(Assert.IsType<ViewResult>(result).Model).Select(t => t.Id).OrderBy(id => id).ToList();

        [Fact]
        public async Task Index_UsuarioNaoCadastrado_RedirecionaParaLogin()
        {
            var fantasma = new Usuario { AspNetUserId = "nao-cadastrado", Nome = "Fantasma" };
            var controller = _fixture.CriarController(fantasma, ehTecnico: false);

            var result = await controller.Index();

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Account", redirect.ControllerName);
        }

        [Fact]
        public async Task Index_Solicitante_VeApenasTicketsQueCriou_MesmoComExibirTodos()
        {
            var dados = _fixture.Dados;
            var meu = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, solicitante: dados.Solicitante);
            SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, solicitante: dados.OutroSolicitante);
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            var padrao = await controller.Index();
            var comExibirTodos = await controller.Index(exibirTodos: true);

            Assert.Equal(new[] { meu.Id }, IdsDoModelo(padrao));
            Assert.Equal(new[] { meu.Id }, IdsDoModelo(comExibirTodos));
            Assert.False((bool)controller.ViewBag.EhTecnico);
        }

        [Fact]
        public async Task Index_TecnicoPadrao_VeCriadosAssumidosEAbertos_NaoVeEmAndamentoDeOutroTecnico()
        {
            var dados = _fixture.Dados;
            var criadoPorMim = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Resolvido, solicitante: dados.Tecnico);
            var assumidoPorMim = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.Tecnico);
            var abertoDeOutro = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, solicitante: dados.OutroSolicitante);
            SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.OutroTecnico);
            SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Pendente, solicitante: dados.OutroSolicitante, tecnico: dados.OutroTecnico);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            var result = await controller.Index();

            Assert.Equal(new[] { criadoPorMim.Id, assumidoPorMim.Id, abertoDeOutro.Id }.OrderBy(id => id), IdsDoModelo(result));
            Assert.True((bool)controller.ViewBag.EhTecnico);
            Assert.False((bool)controller.ViewBag.ExibirTodos);
        }

        [Fact]
        public async Task Index_TecnicoComExibirTodos_VeTodosOsTickets()
        {
            var dados = _fixture.Dados;
            var t1 = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, solicitante: dados.OutroSolicitante);
            var t2 = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.OutroTecnico);
            var t3 = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Resolvido, solicitante: dados.Solicitante);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            var result = await controller.Index(exibirTodos: true);

            Assert.Equal(new[] { t1.Id, t2.Id, t3.Id }.OrderBy(id => id), IdsDoModelo(result));
            Assert.True((bool)controller.ViewBag.ExibirTodos);
        }
    }
}
