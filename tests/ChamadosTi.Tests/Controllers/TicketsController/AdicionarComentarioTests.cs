using System.Text.Json;
using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using ChamadosTi.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ChamadosTi.Tests.Controllers.TicketsControllerTests
{
    public class AdicionarComentarioTests : IDisposable
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
        public async Task AdicionarComentario_UsuarioNaoCadastrado_RedirecionaParaLogin()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto);
            var fantasma = new Usuario { AspNetUserId = "nao-cadastrado", Nome = "Fantasma" };
            var controller = _fixture.CriarController(fantasma, ehTecnico: false);

            // Act
            var result = await controller.AdicionarComentario(ticket.Id, "Olá");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Account", redirect.ControllerName);
            Assert.Empty(_fixture.Contexto.AuditoriasTickets);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task AdicionarComentario_MensagemVazia_RedirecionaParaDetailsComErro(string? mensagem)
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, solicitante: dados.Solicitante);
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            // Act
            var result = await controller.AdicionarComentario(ticket.Id, mensagem!);

            // Assert
            AssertRedirecionaParaDetails(result, ticket.Id);
            Assert.Equal("Error_ComentarioVazio", controller.TempData["ErrorMessage"]);
            Assert.Empty(_fixture.Contexto.AuditoriasTickets);
        }

        [Fact]
        public async Task AdicionarComentario_TicketInexistente_RetornaNotFound()
        {
            // Arrange
            var controller = _fixture.CriarController(_fixture.Dados.Solicitante, ehTecnico: false);

            // Act
            var result = await controller.AdicionarComentario(9999, "Olá");

            // Assert
            Assert.IsType<NotFoundResult>(result);
            Assert.Empty(_fixture.Contexto.AuditoriasTickets);
        }

        [Fact]
        public async Task AdicionarComentario_Solicitante_TicketDeOutroUsuario_RetornaNotFound()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticketDeOutro = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, solicitante: dados.OutroSolicitante);
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            // Act
            var result = await controller.AdicionarComentario(ticketDeOutro.Id, "Olá");

            // Assert
            Assert.IsType<NotFoundResult>(result);
            Assert.Empty(_fixture.Contexto.AuditoriasTickets);
        }

        [Fact]
        public async Task AdicionarComentario_Tecnico_TicketNaoAtribuidoAMim_RetornaNotFound()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.OutroTecnico);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.AdicionarComentario(ticket.Id, "Olá");

            // Assert
            Assert.IsType<NotFoundResult>(result);
            Assert.Empty(_fixture.Contexto.AuditoriasTickets);
        }

        [Fact]
        public async Task AdicionarComentario_Tecnico_TicketAbertoSemTecnicoAtribuido_RetornaNotFound()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, solicitante: dados.Solicitante);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.AdicionarComentario(ticket.Id, "Olá");

            // Assert
            Assert.IsType<NotFoundResult>(result);
            Assert.Empty(_fixture.Contexto.AuditoriasTickets);
        }

        [Fact]
        public async Task AdicionarComentario_Solicitante_TicketProprio_GravaAuditoriaComMensagemAparada()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, solicitante: dados.Solicitante, tecnico: dados.Tecnico);
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            // Act
            var result = await controller.AdicionarComentario(ticket.Id, "  Ainda não funciona  ");

            // Assert
            AssertRedirecionaParaDetails(result, ticket.Id);
            Assert.Equal("Success_ComentarioAdicionado", controller.TempData["SuccessMessage"]);
            var auditoria = Assert.Single(_fixture.Contexto.AuditoriasTickets.Where(a => a.TicketId == ticket.Id));
            var log = JsonSerializer.Deserialize<LogAuditoriaTicket>(auditoria.Log)!;
            Assert.Equal(TicketAcaoLog.Log_NovaInteracao, log.Acao);
            Assert.Equal(dados.Solicitante.Nome, log.Usuario);
            Assert.Equal("Ainda não funciona", log.CamposModificados![TicketAcaoLog.Log_NovaInteracao.ToString()]);
            Assert.Equal(_fixture.Relogio.GetUtcNow().UtcDateTime, auditoria.RegistradoEm);
        }

        [Fact]
        public async Task AdicionarComentario_Tecnico_TicketAtribuidoAMim_GravaAuditoriaDoTecnico()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, solicitante: dados.Solicitante, tecnico: dados.Tecnico);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.AdicionarComentario(ticket.Id, "Peça solicitada");

            // Assert
            AssertRedirecionaParaDetails(result, ticket.Id);
            var auditoria = Assert.Single(_fixture.Contexto.AuditoriasTickets.Where(a => a.TicketId == ticket.Id));
            var log = JsonSerializer.Deserialize<LogAuditoriaTicket>(auditoria.Log)!;
            Assert.Equal(dados.Tecnico.Nome, log.Usuario);
            Assert.Equal("Peça solicitada", log.CamposModificados![TicketAcaoLog.Log_NovaInteracao.ToString()]);
        }
    }
}
