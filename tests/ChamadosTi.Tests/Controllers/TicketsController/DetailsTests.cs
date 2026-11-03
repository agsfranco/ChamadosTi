using System.Text.Json;
using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using ChamadosTi.Tests.Helpers;
using ChamadosTi.ViewModels.Item;
using ChamadosTi.ViewModels.Ticket;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ChamadosTi.Tests.Controllers.TicketsControllerTests
{
    public class DetailsTests : IDisposable
    {
        private readonly TicketsControllerFixture _fixture = new();

        public void Dispose() => _fixture.Dispose();

        private void VincularItem(Ticket ticket, int itemId)
        {
            _fixture.Contexto.ItensTickets.Add(new ItemTicket { TicketId = ticket.Id, ItemId = itemId, Ativo = true });
            _fixture.Contexto.SaveChanges();
        }

        [Fact]
        public async Task Details_UsuarioNaoCadastrado_RedirecionaParaLogin()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto);
            var fantasma = new Usuario { AspNetUserId = "nao-cadastrado", Nome = "Fantasma" };
            var controller = _fixture.CriarController(fantasma, ehTecnico: false);

            // Act
            var result = await controller.Details(ticket.Id);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Account", redirect.ControllerName);
        }

        [Fact]
        public async Task Details_TicketInexistente_RetornaNotFound()
        {
            // Arrange
            var controller = _fixture.CriarController(_fixture.Dados.Solicitante, ehTecnico: false);

            // Act
            var result = await controller.Details(9999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_Solicitante_TicketDeOutroUsuario_RetornaNotFound()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticketDeOutro = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, solicitante: dados.OutroSolicitante);
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            // Act
            var result = await controller.Details(ticketDeOutro.Id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_Solicitante_TicketProprio_RetornaViewComTicketEEhTecnicoFalso()
        {
            // Arrange
            var dados = _fixture.Dados;
            var meu = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, solicitante: dados.Solicitante, tecnico: dados.Tecnico);
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            // Act
            var result = await controller.Details(meu.Id);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Ticket>(view.Model);
            Assert.Equal(meu.Id, model.Id);
            Assert.False((bool)controller.ViewBag.EhTecnico);
        }

        [Fact]
        public async Task Details_Tecnico_TicketAbertoDeOutroSolicitante_RetornaViewComEhTecnicoVerdadeiro()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, solicitante: dados.OutroSolicitante);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.Details(ticket.Id);

            // Assert
            var model = Assert.IsType<Ticket>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(ticket.Id, model.Id);
            Assert.True((bool)controller.ViewBag.EhTecnico);
        }

        [Fact]
        public async Task Details_Tecnico_EmAtendimentoDeOutroTecnico_RetornaViewComEhTecnicoFalso()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.OutroTecnico);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.Details(ticket.Id);

            // Assert
            Assert.IsType<ViewResult>(result);
            Assert.False((bool)controller.ViewBag.EhTecnico);
        }

        [Fact]
        public async Task Details_Tecnico_EmAtendimentoAtribuidoAMim_RetornaViewComEhTecnicoVerdadeiro()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.EmAtendimento, tecnico: dados.Tecnico);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            var result = await controller.Details(ticket.Id);

            // Assert
            Assert.IsType<ViewResult>(result);
            Assert.True((bool)controller.ViewBag.EhTecnico);
        }

        [Fact]
        public async Task Details_Solicitante_RecebeApenasItensDoSetorDoTicket()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, solicitante: dados.Solicitante);
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            // Act
            await controller.Details(ticket.Id);

            // Assert
            var itens = (List<ItemDTO>)controller.ViewBag.Itens;
            var item = Assert.Single(itens);
            Assert.Equal(dados.ItemDoSetorComum.Id, item.Id);
        }

        [Fact]
        public async Task Details_Tecnico_RecebeTodosOsItens()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, solicitante: dados.OutroSolicitante);
            var controller = _fixture.CriarController(dados.Tecnico, ehTecnico: true);

            // Act
            await controller.Details(ticket.Id);

            // Assert
            var itens = (List<ItemDTO>)controller.ViewBag.Itens;
            Assert.Equal(2, itens.Count);
        }

        [Fact]
        public async Task Details_RecebeListaDePrioridades()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, solicitante: dados.Solicitante);
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            // Act
            await controller.Details(ticket.Id);

            // Assert
            var prioridades = (List<Prioridade>)controller.ViewBag.Prioridades;
            Assert.Equal(2, prioridades.Count);
        }

        [Fact]
        public async Task Details_ComItemVinculado_MontaHistoricoComParecerDoChamadoAnterior()
        {
            // Arrange
            var dados = _fixture.Dados;
            var anterior = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Resolvido, solicitante: dados.OutroSolicitante, tecnico: dados.Tecnico);
            VincularItem(anterior, dados.ItemDoSetorComum.Id);

            var log = new LogAuditoriaTicket
            {
                Acao = TicketAcaoLog.Log_ChamadoResolvido,
                Usuario = dados.Tecnico.Nome,
                CamposModificados = new Dictionary<string, string> { { TicketAcaoLog.Log_StatusAlterado.ToString(), "Status_Resolvido" } },
                Mensagem = "Fonte substituída"
            };
            _fixture.Contexto.AuditoriasTickets.Add(new AuditoriaTicket
            {
                TicketId = anterior.Id,
                Log = JsonSerializer.Serialize(log),
                RegistradoEm = _fixture.Relogio.GetUtcNow().UtcDateTime,
                Ativo = true
            });
            _fixture.Contexto.SaveChanges();

            var atual = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, solicitante: dados.Solicitante);
            VincularItem(atual, dados.ItemDoSetorComum.Id);
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            // Act
            await controller.Details(atual.Id);

            // Assert
            var historico = Assert.IsType<List<TicketHistoricoAtivoDTO>>(controller.ViewBag.HistoricoAtivo);
            var entrada = Assert.Single(historico);
            Assert.Equal(anterior.Id, entrada.Id);
            Assert.Equal("Fonte substituída", entrada.ParecerTecnico);
            Assert.Equal("Status_Resolvido", entrada.StatusNome);
        }

        [Fact]
        public async Task Details_SemItensVinculados_HistoricoVazio()
        {
            // Arrange
            var dados = _fixture.Dados;
            var ticket = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Aberto, solicitante: dados.Solicitante);
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            // Act
            await controller.Details(ticket.Id);

            // Assert
            var historico = Assert.IsType<List<TicketHistoricoAtivoDTO>>(controller.ViewBag.HistoricoAtivo);
            Assert.Empty(historico);
        }
    }
}
