using ChamadosTi.Controllers;
using ChamadosTi.Models.Enums;
using Xunit;

namespace ChamadosTi.Tests.Helpers
{
    public class FixtureTests : IDisposable
    {
        private readonly TicketsControllerFixture _fixture = new();

        public void Dispose() => _fixture.Dispose();

        [Fact]
        public void StatusDoBancoSaoCriadosPeloHasData()
        {
            var codigos = _fixture.Contexto.Status.Select(s => s.Codigo).OrderBy(c => c).ToList();

            Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, codigos);
        }

        [Fact]
        public void SetorTiTemExpedienteParaTodosOsDias()
        {
            var horarios = _fixture.Contexto.SetoresHorarios
                .Where(sh => sh.SetorId == _fixture.Dados.SetorTi.Id)
                .Count();

            Assert.Equal(7, horarios);
        }

        [Fact]
        public void CriarControllerMontaAsDependenciasDaController()
        {
            var controller = _fixture.CriarController(_fixture.Dados.Solicitante, ehTecnico: false);

            Assert.IsType<TicketsController>(controller);
            Assert.NotNull(controller.TempData);
            Assert.NotNull(controller.ViewData);
            Assert.Equal(_fixture.Dados.Solicitante.AspNetUserId, controller.User.Claims.Single(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Value);
        }

        [Fact]
        public void CriarTicketGravaTicketNoStatusInformado()
        {
            var ticket = SeedData.CriarTicket(_fixture.Contexto, _fixture.Dados, TicketStatus.Pendente, tecnico: _fixture.Dados.Tecnico);

            var gravado = _fixture.Contexto.Tickets.Single(t => t.Id == ticket.Id);
            Assert.Equal((int)TicketStatus.Pendente, _fixture.Contexto.Status.Single(s => s.Id == gravado.StatusId).Codigo);
            Assert.Equal(_fixture.Dados.Tecnico.Id, gravado.UsuarioTecnicoId);
        }

        [Fact]
        public void UsuariosDoSeedEstaoAtivosEVisiveisPeloFiltroGlobal()
        {
            Assert.True(_fixture.Contexto.Usuarios.Any(u => u.Id == _fixture.Dados.Solicitante.Id));
            Assert.True(_fixture.Contexto.Usuarios.Any(u => u.Id == _fixture.Dados.Tecnico.Id));
        }
    }
}
