using System.Text.Json;
using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using ChamadosTi.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ChamadosTi.Tests.Controllers.TicketsControllerTests
{
    public class RetomarAtendimentoTests : IDisposable
    {
        private readonly TicketsControllerFixture _fixture = new();

        public void Dispose() => _fixture.Dispose();

        private static void AssertRedirecionaParaDetails(IActionResult result, int id)
        {
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.Equal(id, redirect.RouteValues!["id"]);
        }

        private void RegistrarPausa(Ticket ticket, Usuario usuario)
        {
            var log = new LogAuditoriaTicket { Acao = TicketAcaoLog.Log_ChamadoPendente, Usuario = usuario.Nome };
            _fixture.Contexto.AuditoriasTickets.Add(new AuditoriaTicket
            {
                TicketId = ticket.Id,
                Log = JsonSerializer.Serialize(log),
                RegistradoEm = _fixture.Relogio.GetUtcNow().UtcDateTime,
                Ativo = true
            });
            _fixture.Contexto.SaveChanges();
        }

        [Fact]
        public async Task RetomarAtendimento_UsuarioNaoCadastrado_RedirecionaParaLogin()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Pendente, tecnico: dados.Tecnico);
            var fantasma = new Usuario { AspNetUserId = "nao-cadastrado", Nome = "Fantasma" };
            var controller = _fixture.CriarController(fantasma, ehTecnico: true);

            // Act
            var result = await controller.RetomarAtendimento(ticket.Id);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Account", redirect.ControllerName);
        }

        [Fact]
        public async Task RetomarAtendimento_Solicitante_RetornaNotFound()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Pendente, solicitante: dados.Solicitante, tecnico: dados.Tecnico);
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            // Act
            var result = await controller.RetomarAtendimento(ticket.Id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task RetomarAtendimento_TicketInexistente_RetornaNotFound()
        {
            // Arrange
            var controller = _fixture.CriarController(_fixture.Dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.RetomarAtendimento(9999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task RetomarAtendimento_TecnicoNaoAtribuido_RetornaNotFound()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Pendente, tecnico: dados.OutroTecnico);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.RetomarAtendimento(ticket.Id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            Assert.Equal(_fixture.Contexto.Status.Single(s => s.Codigo == (int)TicketStatus.Pendente).Id, _fixture.Contexto.Tickets.Single().StatusId);
        }

        [Fact]
        public async Task RetomarAtendimento_StatusDiferenteDePendente_RedirecionaParaDetailsComErro()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.Tecnico);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.RetomarAtendimento(ticket.Id);

            // Assert
            AssertRedirecionaParaDetails(result, ticket.Id);
            Assert.Equal("Error_StatusInvalidoParaTramitar", controller.TempData["ErrorMessage"]);
            Assert.Empty(_fixture.Contexto.AuditoriasTickets);
        }

        [Fact]
        public async Task RetomarAtendimento_SemAuditoriaDePausa_NaoEstendePrazoDeResolucao()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Pendente, tecnico: dados.Tecnico);
            var prazoOriginal = _fixture.Contexto.Tickets.Single().PrazoResolucaoSLA;
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.RetomarAtendimento(ticket.Id);

            // Assert
            AssertRedirecionaParaDetails(result, ticket.Id);
            Assert.Equal("Success_ChamadoRetomado", controller.TempData["SuccessMessage"]);
            var salvo = _fixture.Contexto.Tickets.Single();
            Assert.Equal(_fixture.Contexto.Status.Single(s => s.Codigo == (int)TicketStatus.EmAtendimento).Id, salvo.StatusId);
            Assert.Equal(prazoOriginal, salvo.PrazoResolucaoSLA);
        }

        [Fact]
        public async Task RetomarAtendimento_ComPausa_EstendePrazoDeResolucaoPeloTempoPausado()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Pendente, tecnico: dados.Tecnico);
            var prazoOriginal = _fixture.Contexto.Tickets.Single().PrazoResolucaoSLA;
            RegistrarPausa(ticket, dados.Tecnico);
            _fixture.Relogio.Advance(TimeSpan.FromHours(2));
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            await controller.RetomarAtendimento(ticket.Id);

            // Assert
            var salvo = _fixture.Contexto.Tickets.Single();
            Assert.Equal(prazoOriginal.AddMinutes(120), salvo.PrazoResolucaoSLA);
        }

        [Fact]
        public async Task RetomarAtendimento_ComPausa_GravaAuditoriaDeRetomadaComPrazoEstendido()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Pendente, tecnico: dados.Tecnico);
            RegistrarPausa(ticket, dados.Tecnico);
            _fixture.Relogio.Advance(TimeSpan.FromHours(2));
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            await controller.RetomarAtendimento(ticket.Id);

            // Assert
            var auditoria = _fixture.Contexto.AuditoriasTickets
                .Where(a => a.TicketId == ticket.Id)
                .AsEnumerable()
                .Single(a => JsonSerializer.Deserialize<LogAuditoriaTicket>(a.Log)!.Acao == TicketAcaoLog.Log_ChamadoRetomado);
            var log = JsonSerializer.Deserialize<LogAuditoriaTicket>(auditoria.Log)!;
            Assert.Equal(dados.Tecnico.Nome, log.Usuario);
            Assert.Equal("Status_EmAtendimento", log.CamposModificados![TicketAcaoLog.Log_StatusAlterado.ToString()]);
            Assert.True(log.CamposModificados.ContainsKey(TicketAcaoLog.Log_PrazoResolucaoEstendido.ToString()));
        }
    }
}
