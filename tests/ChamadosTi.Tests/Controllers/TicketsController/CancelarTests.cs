using System.Text.Json;
using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using ChamadosTi.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ChamadosTi.Tests.Controllers.TicketsControllerTests
{
    public class CancelarTests : IDisposable
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
        public async Task Cancelar_UsuarioNaoCadastrado_RedirecionaParaLogin()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, solicitante: dados.Solicitante);
            var fantasma = new Usuario { AspNetUserId = "nao-cadastrado", Nome = "Fantasma" };
            var controller = _fixture.CriarController(fantasma, ehTecnico: false);

            // Act
            var result = await controller.Cancelar(ticket.Id, "Não é mais necessário");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Account", redirect.ControllerName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task Cancelar_JustificativaVazia_RedirecionaParaDetailsComErro(string? justificativa)
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, solicitante: dados.Solicitante);
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            // Act
            var result = await controller.Cancelar(ticket.Id, justificativa!);

            // Assert
            AssertRedirecionaParaDetails(result, ticket.Id);
            Assert.Equal("Error_JustificativaObrigatoria", controller.TempData["ErrorMessage"]);
            Assert.Equal(_fixture.Contexto.Status.Single(s => s.Codigo == (int)TicketStatus.Aberto).Id, _fixture.Contexto.Tickets.Single().StatusId);
        }

        [Fact]
        public async Task Cancelar_TicketInexistente_RetornaNotFound()
        {
            // Arrange
            var controller = _fixture.CriarController(_fixture.Dados.Solicitante, ehTecnico: false);

            // Act
            var result = await controller.Cancelar(9999, "Não é mais necessário");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Cancelar_Solicitante_TicketDeOutroUsuario_RetornaNotFound()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticketDeOutro = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, solicitante: dados.OutroSolicitante);
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            // Act
            var result = await controller.Cancelar(ticketDeOutro.Id, "Não é mais necessário");

            // Assert
            Assert.IsType<NotFoundResult>(result);
            Assert.Equal(_fixture.Contexto.Status.Single(s => s.Codigo == (int)TicketStatus.Aberto).Id, _fixture.Contexto.Tickets.Single().StatusId);
        }

        [Fact]
        public async Task Cancelar_Solicitante_TicketProprioJaEmAtendimento_RetornaNotFound()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, solicitante: dados.Solicitante, tecnico: dados.Tecnico);
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            // Act
            var result = await controller.Cancelar(ticket.Id, "Não é mais necessário");

            // Assert
            Assert.IsType<NotFoundResult>(result);
            Assert.Empty(_fixture.Contexto.AuditoriasTickets);
        }

        [Fact]
        public async Task Cancelar_Tecnico_TicketEmAtendimentoDeOutroTecnico_RetornaNotFound()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.OutroTecnico);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.Cancelar(ticket.Id, "Não é mais necessário");

            // Assert
            Assert.IsType<NotFoundResult>(result);
            Assert.Empty(_fixture.Contexto.AuditoriasTickets);
        }

        [Fact]
        public async Task Cancelar_Tecnico_TicketAbertoSemTecnico_CancelaChamado()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, solicitante: dados.OutroSolicitante);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.Cancelar(ticket.Id, "Duplicado");

            // Assert
            AssertRedirecionaParaDetails(result, ticket.Id);
            Assert.Equal("Success_ChamadoCancelado", controller.TempData["SuccessMessage"]);
            Assert.Equal(_fixture.Contexto.Status.Single(s => s.Codigo == (int)TicketStatus.Cancelado).Id, _fixture.Contexto.Tickets.Single().StatusId);
        }

        [Fact]
        public async Task Cancelar_Solicitante_TicketAbertoProprio_CancelaEDefineDataFechamento()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, solicitante: dados.Solicitante);
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            // Act
            var result = await controller.Cancelar(ticket.Id, "Não é mais necessário");

            // Assert
            AssertRedirecionaParaDetails(result, ticket.Id);
            Assert.Equal("Success_ChamadoCancelado", controller.TempData["SuccessMessage"]);
            var salvo = _fixture.Contexto.Tickets.Single();
            Assert.Equal(_fixture.Contexto.Status.Single(s => s.Codigo == (int)TicketStatus.Cancelado).Id, salvo.StatusId);
            Assert.Equal(_fixture.Relogio.GetUtcNow().UtcDateTime, salvo.DataFechamento);
        }

        [Fact]
        public async Task Cancelar_TecnicoAtribuido_EmAtendimento_CancelaChamado()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, solicitante: dados.Solicitante, tecnico: dados.Tecnico);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.Cancelar(ticket.Id, "Equipamento devolvido ao fornecedor");

            // Assert
            AssertRedirecionaParaDetails(result, ticket.Id);
            Assert.Equal(_fixture.Contexto.Status.Single(s => s.Codigo == (int)TicketStatus.Cancelado).Id, _fixture.Contexto.Tickets.Single().StatusId);
        }

        [Fact]
        public async Task Cancelar_StatusResolvido_RedirecionaParaDetailsComErro()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Resolvido, solicitante: dados.Solicitante, tecnico: dados.Tecnico);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.Cancelar(ticket.Id, "Não é mais necessário");

            // Assert
            AssertRedirecionaParaDetails(result, ticket.Id);
            Assert.Equal("Error_StatusInvalidoParaCancelar", controller.TempData["ErrorMessage"]);
            Assert.Equal(_fixture.Contexto.Status.Single(s => s.Codigo == (int)TicketStatus.Resolvido).Id, _fixture.Contexto.Tickets.Single().StatusId);
        }

        [Fact]
        public async Task Cancelar_GravaAuditoriaComJustificativaAparada()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, solicitante: dados.Solicitante);
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            // Act
            await controller.Cancelar(ticket.Id, "  Não é mais necessário  ");

            // Assert
            var auditoria = Assert.Single(_fixture.Contexto.AuditoriasTickets.Where(a => a.TicketId == ticket.Id));
            var log = JsonSerializer.Deserialize<LogAuditoriaTicket>(auditoria.Log)!;
            Assert.Equal(TicketAcaoLog.Log_ChamadoCancelado, log.Acao);
            Assert.Equal(dados.Solicitante.Nome, log.Usuario);
            Assert.Equal("Não é mais necessário", log.Mensagem);
            Assert.Equal("Status_Cancelado", log.CamposModificados![TicketAcaoLog.Log_StatusAlterado.ToString()]);
        }
    }
}
