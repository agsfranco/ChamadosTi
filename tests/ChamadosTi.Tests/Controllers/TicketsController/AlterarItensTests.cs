using System.Text.Json;
using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using ChamadosTi.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ChamadosTi.Tests.Controllers.TicketsControllerTests
{
    public class AlterarItensTests : IDisposable
    {
        private readonly TicketsControllerFixture _fixture = new();

        public void Dispose() => _fixture.Dispose();

        private static void AssertRedirecionaParaDetails(IActionResult result, int id)
        {
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.Equal(id, redirect.RouteValues!["id"]);
        }

        private void VincularItem(Ticket ticket, int itemId)
        {
            _fixture.Contexto.ItensTickets.Add(new ItemTicket { TicketId = ticket.Id, ItemId = itemId, Ativo = true });
            _fixture.Contexto.SaveChanges();
        }

        [Fact]
        public async Task AlterarItens_UsuarioNaoCadastrado_RedirecionaParaLogin()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.Tecnico);
            var fantasma = new Usuario { AspNetUserId = "nao-cadastrado", Nome = "Fantasma" };
            var controller = _fixture.CriarController(fantasma, ehTecnico: true);

            // Act
            var result = await controller.AlterarItens(ticket.Id, new List<int> { dados.ItemDoSetorTi.Id });

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Account", redirect.ControllerName);
        }

        [Fact]
        public async Task AlterarItens_TicketInexistente_RetornaNotFound()
        {
            // Arrange
            var controller = _fixture.CriarController(_fixture.Dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.AlterarItens(9999, new List<int> { _fixture.Dados.ItemDoSetorTi.Id });

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AlterarItens_TecnicoNaoAtribuidoEmAtendimento_RetornaNotFound()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.OutroTecnico);
            VincularItem(ticket, dados.ItemDoSetorComum.Id);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.AlterarItens(ticket.Id, new List<int> { dados.ItemDoSetorTi.Id });

            // Assert
            Assert.IsType<NotFoundResult>(result);
            var vinculo = Assert.Single(_fixture.Contexto.ItensTickets.Where(it => it.TicketId == ticket.Id));
            Assert.Equal(dados.ItemDoSetorComum.Id, vinculo.ItemId);
        }

        [Fact]
        public async Task AlterarItens_StatusResolvido_RedirecionaParaDetailsComErro()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Resolvido, tecnico: dados.Tecnico);
            VincularItem(ticket, dados.ItemDoSetorComum.Id);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.AlterarItens(ticket.Id, new List<int> { dados.ItemDoSetorTi.Id });

            // Assert
            AssertRedirecionaParaDetails(result, ticket.Id);
            Assert.Equal("Error_StatusInvalidoParaAlterarItens", controller.TempData["ErrorMessage"]);
            var vinculo = Assert.Single(_fixture.Contexto.ItensTickets.Where(it => it.TicketId == ticket.Id));
            Assert.Equal(dados.ItemDoSetorComum.Id, vinculo.ItemId);
        }

        [Fact]
        public async Task AlterarItens_EmAtendimentoAtribuidoAMim_SubstituiItensVinculados()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.Tecnico);
            VincularItem(ticket, dados.ItemDoSetorComum.Id);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.AlterarItens(ticket.Id, new List<int> { dados.ItemDoSetorTi.Id });

            // Assert
            AssertRedirecionaParaDetails(result, ticket.Id);
            Assert.Equal("Success_ItensAlterados", controller.TempData["SuccessMessage"]);
            var vinculo = Assert.Single(_fixture.Contexto.ItensTickets.Where(it => it.TicketId == ticket.Id));
            Assert.Equal(dados.ItemDoSetorTi.Id, vinculo.ItemId);
            Assert.True(vinculo.Ativo);
        }

        [Fact]
        public async Task AlterarItens_ListaVazia_RemoveTodosOsItensVinculados()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.Tecnico);
            VincularItem(ticket, dados.ItemDoSetorComum.Id);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            await controller.AlterarItens(ticket.Id, new List<int>());

            // Assert
            Assert.Empty(_fixture.Contexto.ItensTickets.Where(it => it.TicketId == ticket.Id));
        }

        [Fact]
        public async Task AlterarItens_ListaVazia_GravaAuditoriaComNenhum()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.Tecnico);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            await controller.AlterarItens(ticket.Id, new List<int>());

            // Assert
            var auditoria = Assert.Single(_fixture.Contexto.AuditoriasTickets.Where(a => a.TicketId == ticket.Id));
            var log = JsonSerializer.Deserialize<LogAuditoriaTicket>(auditoria.Log)!;
            Assert.Equal("Nenhum", log.CamposModificados![TicketAcaoLog.Log_ItensAlterados.ToString()]);
        }

        [Fact]
        public async Task AlterarItens_GravaAuditoriaComIdsSelecionados()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.Tecnico);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            await controller.AlterarItens(ticket.Id, new List<int> { dados.ItemDoSetorComum.Id, dados.ItemDoSetorTi.Id });

            // Assert
            var auditoria = Assert.Single(_fixture.Contexto.AuditoriasTickets.Where(a => a.TicketId == ticket.Id));
            var log = JsonSerializer.Deserialize<LogAuditoriaTicket>(auditoria.Log)!;
            Assert.Equal(TicketAcaoLog.Log_ItensAlterados, log.Acao);
            Assert.Equal(dados.Tecnico.Nome, log.Usuario);
            Assert.Equal($"{dados.ItemDoSetorComum.Id}, {dados.ItemDoSetorTi.Id}", log.CamposModificados![TicketAcaoLog.Log_ItensAlterados.ToString()]);
        }
    }
}
