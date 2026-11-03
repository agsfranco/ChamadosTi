using System.Text.Json;
using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using ChamadosTi.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ChamadosTi.Tests.Controllers.TicketsControllerTests
{
    public class MoverParaPendenteTests : IDisposable
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
        public async Task MoverParaPendente_UsuarioNaoCadastrado_RedirecionaParaLogin()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.Tecnico);
            var fantasma = new Usuario { AspNetUserId = "nao-cadastrado", Nome = "Fantasma" };
            var controller = _fixture.CriarController(fantasma, ehTecnico: true);

            // Act
            var result = await controller.MoverParaPendente(ticket.Id);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Account", redirect.ControllerName);
        }

        [Fact]
        public async Task MoverParaPendente_Solicitante_RetornaNotFound()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, solicitante: dados.Solicitante, tecnico: dados.Tecnico);
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            // Act
            var result = await controller.MoverParaPendente(ticket.Id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            Assert.Equal(_fixture.Contexto.Status.Single(s => s.Codigo == (int)TicketStatus.EmAtendimento).Id, _fixture.Contexto.Tickets.Single().StatusId);
        }

        [Fact]
        public async Task MoverParaPendente_TicketInexistente_RetornaNotFound()
        {
            // Arrange
            var controller = _fixture.CriarController(_fixture.Dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.MoverParaPendente(9999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task MoverParaPendente_TecnicoNaoAtribuido_RetornaNotFound()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.OutroTecnico);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.MoverParaPendente(ticket.Id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            Assert.Equal(dados.OutroTecnico.Id, _fixture.Contexto.Tickets.Single().UsuarioTecnicoId);
        }

        [Fact]
        public async Task MoverParaPendente_StatusDiferenteDeEmAtendimento_RedirecionaParaDetailsComErro()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Pendente, tecnico: dados.Tecnico);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.MoverParaPendente(ticket.Id);

            // Assert
            AssertRedirecionaParaDetails(result, ticket.Id);
            Assert.Equal("Error_StatusInvalidoParaTramitar", controller.TempData["ErrorMessage"]);
            Assert.Empty(_fixture.Contexto.AuditoriasTickets);
        }

        [Fact]
        public async Task MoverParaPendente_EmAtendimentoAtribuidoAMim_MudaStatusParaPendente()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.Tecnico);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.MoverParaPendente(ticket.Id);

            // Assert
            AssertRedirecionaParaDetails(result, ticket.Id);
            Assert.Equal("Success_ChamadoPendente", controller.TempData["SuccessMessage"]);
            var salvo = _fixture.Contexto.Tickets.Single();
            Assert.Equal(_fixture.Contexto.Status.Single(s => s.Codigo == (int)TicketStatus.Pendente).Id, salvo.StatusId);
        }

        [Fact]
        public async Task MoverParaPendente_EmAtendimentoAtribuidoAMim_GravaAuditoriaDePausa()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.Tecnico);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            await controller.MoverParaPendente(ticket.Id);

            // Assert
            var auditoria = Assert.Single(_fixture.Contexto.AuditoriasTickets.Where(a => a.TicketId == ticket.Id));
            var log = JsonSerializer.Deserialize<LogAuditoriaTicket>(auditoria.Log)!;
            Assert.Equal(TicketAcaoLog.Log_ChamadoPendente, log.Acao);
            Assert.Equal(dados.Tecnico.Nome, log.Usuario);
            Assert.Equal(_fixture.Relogio.GetUtcNow().UtcDateTime, auditoria.RegistradoEm);
        }
    }
}
