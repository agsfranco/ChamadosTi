using System.Text.Json;
using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using ChamadosTi.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ChamadosTi.Tests.Controllers.TicketsControllerTests
{
    public class AssumirTests : IDisposable
    {
        private readonly TicketsControllerFixture _fixture = new();

        public void Dispose() => _fixture.Dispose();

        private static void AssertRedirecionaParaDetails(IActionResult result, int id)
        {
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.Equal(id, redirect.RouteValues!["id"]);
        }

        [Fact]
        public async Task Assumir_UsuarioNaoCadastrado_RedirecionaParaLogin()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto);
            var fantasma = new Usuario { AspNetUserId = "nao-cadastrado", Nome = "Fantasma" };
            var controller = _fixture.CriarController(fantasma, ehTecnico: true);

            // Act
            var result = await controller.Assumir(ticket.Id);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Account", redirect.ControllerName);
        }

        [Fact]
        public async Task Assumir_UsuarioNaoTecnico_RetornaNotFound()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto);
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            // Act
            var result = await controller.Assumir(ticket.Id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            Assert.Null(_fixture.Contexto.Tickets.Single().UsuarioTecnicoId);
        }

        [Fact]
        public async Task Assumir_TicketInexistente_RetornaNotFound()
        {
            // Arrange
            var controller = _fixture.CriarController(_fixture.Dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.Assumir(9999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Assumir_TicketJaEmAtendimento_RedirecionaParaDetailsComErro()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.OutroTecnico);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.Assumir(ticket.Id);

            // Assert
            AssertRedirecionaParaDetails(result, ticket.Id);
            Assert.Equal("Error_ChamadoJaAssumidoOuFechado", controller.TempData["ErrorMessage"]);
            var salvo = _fixture.Contexto.Tickets.Single();
            Assert.Equal(dados.OutroTecnico.Id, salvo.UsuarioTecnicoId);
            Assert.Equal(_fixture.Contexto.Status.Single(s => s.Codigo == (int)TicketStatus.EmAtendimento).Id, salvo.StatusId);
        }

        [Fact]
        public async Task Assumir_TicketAberto_AtribuiTecnicoEMudaStatusParaEmAtendimento()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.Assumir(ticket.Id);

            // Assert
            AssertRedirecionaParaDetails(result, ticket.Id);
            var salvo = _fixture.Contexto.Tickets.Single();
            Assert.Equal(dados.Tecnico.Id, salvo.UsuarioTecnicoId);
            Assert.Equal(_fixture.Contexto.Status.Single(s => s.Codigo == (int)TicketStatus.EmAtendimento).Id, salvo.StatusId);
            Assert.Equal("Success_ChamadoAssumido", controller.TempData["SuccessMessage"]);
        }

        [Fact]
        public async Task Assumir_TicketAberto_GravaAuditoriaComStatusETecnico()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            await controller.Assumir(ticket.Id);

            // Assert
            var auditoria = Assert.Single(_fixture.Contexto.AuditoriasTickets.Where(a => a.TicketId == ticket.Id));
            var log = JsonSerializer.Deserialize<LogAuditoriaTicket>(auditoria.Log)!;
            Assert.Equal(TicketAcaoLog.Log_ChamadoAssumido, log.Acao);
            Assert.Equal(dados.Tecnico.Nome, log.Usuario);
            Assert.Equal("Status_EmAtendimento", log.CamposModificados![TicketAcaoLog.Log_StatusAlterado.ToString()]);
            Assert.Equal(dados.Tecnico.Nome, log.CamposModificados[TicketAcaoLog.Log_TecnicoAtribuido.ToString()]);
            Assert.Equal(_fixture.Relogio.GetUtcNow().UtcDateTime, auditoria.RegistradoEm);
        }
    }
}
