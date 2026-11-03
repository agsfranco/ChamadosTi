using System.Text.Json;
using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using ChamadosTi.Tests.Helpers;
using ChamadosTi.Utils;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ChamadosTi.Tests.Controllers.TicketsControllerTests
{
    public class AlterarPrioridadeTests : IDisposable
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
        public async Task AlterarPrioridade_UsuarioNaoCadastrado_RedirecionaParaLogin()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.Tecnico);
            var fantasma = new Usuario { AspNetUserId = "nao-cadastrado", Nome = "Fantasma" };
            var controller = _fixture.CriarController(fantasma, ehTecnico: true);

            // Act
            var result = await controller.AlterarPrioridade(ticket.Id, dados.PrioridadeAlta.Id);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Account", redirect.ControllerName);
        }

        [Fact]
        public async Task AlterarPrioridade_TicketInexistente_RetornaNotFound()
        {
            // Arrange
            var controller = _fixture.CriarController(_fixture.Dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.AlterarPrioridade(9999, _fixture.Dados.PrioridadeAlta.Id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AlterarPrioridade_TecnicoNaoAtribuidoEmAtendimento_RetornaNotFound()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.OutroTecnico);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.AlterarPrioridade(ticket.Id, dados.PrioridadeAlta.Id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            Assert.Equal(dados.PrioridadePadrao.Id, _fixture.Contexto.Tickets.Single().PrioridadeId);
        }

        [Fact]
        public async Task AlterarPrioridade_StatusResolvido_RedirecionaParaDetailsComErro()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Resolvido, tecnico: dados.Tecnico);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.AlterarPrioridade(ticket.Id, dados.PrioridadeAlta.Id);

            // Assert
            AssertRedirecionaParaDetails(result, ticket.Id);
            Assert.Equal("Error_StatusInvalidoParaAlterarPrioridade", controller.TempData["ErrorMessage"]);
            Assert.Equal(dados.PrioridadePadrao.Id, _fixture.Contexto.Tickets.Single().PrioridadeId);
        }

        [Fact]
        public async Task AlterarPrioridade_PrioridadeInexistente_RetornaNotFound()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.Tecnico);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.AlterarPrioridade(ticket.Id, 9999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            Assert.Equal(dados.PrioridadePadrao.Id, _fixture.Contexto.Tickets.Single().PrioridadeId);
        }

        [Fact]
        public async Task AlterarPrioridade_EmAtendimentoAtribuidoAMim_RecalculaApenasPrazoDeResolucao()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.Tecnico);
            var prazoRespostaOriginal = ticket.PrazoRespostaSLA;
            var horarios = _fixture.Contexto.Horarios.ToList();
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.AlterarPrioridade(ticket.Id, dados.PrioridadeAlta.Id);

            // Assert
            AssertRedirecionaParaDetails(result, ticket.Id);
            Assert.Equal("Success_PrioridadeAlterada", controller.TempData["SuccessMessage"]);
            var salvo = _fixture.Contexto.Tickets.Single();
            Assert.Equal(dados.PrioridadeAlta.Id, salvo.PrioridadeId);
            Assert.Equal(
                SlaCalculator.CalcularPrazoSlaReal(new List<DateOnly>(), horarios, ticket.DataAbertura, dados.PrioridadeAlta.TempoResolucaoPadrao),
                salvo.PrazoResolucaoSLA);
            Assert.Equal(prazoRespostaOriginal, salvo.PrazoRespostaSLA);
        }

        [Fact]
        public async Task AlterarPrioridade_EmAtendimentoAtribuidoAMim_GravaAuditoriaSemPrazoDeResposta()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.Tecnico);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            await controller.AlterarPrioridade(ticket.Id, dados.PrioridadeAlta.Id);

            // Assert
            var auditoria = Assert.Single(_fixture.Contexto.AuditoriasTickets.Where(a => a.TicketId == ticket.Id));
            var log = JsonSerializer.Deserialize<LogAuditoriaTicket>(auditoria.Log)!;
            Assert.Equal(TicketAcaoLog.Log_PrioridadeAlterada, log.Acao);
            Assert.Equal(dados.Tecnico.Nome, log.Usuario);
            Assert.Equal(dados.PrioridadeAlta.Nome, log.CamposModificados![TicketAcaoLog.Log_PrioridadeAlterada.ToString()]);
            Assert.True(log.CamposModificados.ContainsKey(TicketAcaoLog.Log_PrazoResolucao.ToString()));
            Assert.False(log.CamposModificados.ContainsKey(TicketAcaoLog.Log_PrazoResposta.ToString()));
        }

        [Fact]
        public async Task AlterarPrioridade_TicketAbertoSemTecnico_RecalculaPrazoDeRespostaEResolucao()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, solicitante: dados.Solicitante);
            var horarios = _fixture.Contexto.Horarios.ToList();
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            await controller.AlterarPrioridade(ticket.Id, dados.PrioridadeAlta.Id);

            // Assert
            var salvo = _fixture.Contexto.Tickets.Single();
            Assert.Equal(
                SlaCalculator.CalcularPrazoSlaReal(new List<DateOnly>(), horarios, ticket.DataAbertura, dados.PrioridadeAlta.TempoRespostaPadrao),
                salvo.PrazoRespostaSLA);
            Assert.Equal(
                SlaCalculator.CalcularPrazoSlaReal(new List<DateOnly>(), horarios, ticket.DataAbertura, dados.PrioridadeAlta.TempoResolucaoPadrao),
                salvo.PrazoResolucaoSLA);

            var auditoria = Assert.Single(_fixture.Contexto.AuditoriasTickets.Where(a => a.TicketId == ticket.Id));
            var log = JsonSerializer.Deserialize<LogAuditoriaTicket>(auditoria.Log)!;
            Assert.True(log.CamposModificados!.ContainsKey(TicketAcaoLog.Log_PrazoResposta.ToString()));
        }
    }
}
