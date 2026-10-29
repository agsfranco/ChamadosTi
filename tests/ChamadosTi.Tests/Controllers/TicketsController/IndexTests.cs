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
    }
}
