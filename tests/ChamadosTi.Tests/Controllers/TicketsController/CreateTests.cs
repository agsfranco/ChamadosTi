using System.Text.Json;
using ChamadosTi.Controllers;
using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using ChamadosTi.Tests.Helpers;
using ChamadosTi.Utils;
using ChamadosTi.ViewModels.Categoria;
using ChamadosTi.ViewModels.Item;
using ChamadosTi.ViewModels.Ticket;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ChamadosTi.Tests.Controllers.TicketsControllerTests
{
    public class CreateTests : IDisposable
    {
        private readonly TicketsControllerFixture _fixture = new();

        public void Dispose() => _fixture.Dispose();

        private TicketCreateViewModel ModeloValido(params int[] itens) => new()
        {
            Titulo = "Notebook não liga",
            Comentario = "Notebook não liga desde ontem pela manhã.",
            CategoriaId = _fixture.Dados.Categoria.Id,
            ItensIdsSelecionados = itens.ToList()
        };

        private static void AssertRedirecionaPara(IActionResult result, string acao, string controlador)
        {
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(acao, redirect.ActionName);
            Assert.Equal(controlador, redirect.ControllerName);
        }

        private static bool ContemErro(TicketsController controller, string chave)
            => controller.ModelState[string.Empty]?.Errors.Any(e => e.ErrorMessage == chave) == true;

        // ---------- GET ----------

        [Fact]
        public async Task CreateGet_UsuarioNaoCadastrado_RedirecionaParaLogin()
        {
            var fantasma = new Usuario { AspNetUserId = "nao-cadastrado", Nome = "Fantasma" };
            var controller = _fixture.CriarController(fantasma, ehTecnico: false);

            var result = await controller.Create((int?)null);

            AssertRedirecionaPara(result, "Login", "Account");
        }

        [Fact]
        public async Task CreateGet_Tecnico_RecebeTodosOsItensECategorias()
        {
            var controller = _fixture.CriarController(_fixture.Dados.Tecnico, ehTecnico: true);

            var result = await controller.Create((int?)null);

            Assert.IsType<TicketCreateViewModel>(Assert.IsType<ViewResult>(result).Model);
            var itens = (List<ItemDTO>)controller.ViewBag.Itens;
            Assert.Equal(2, itens.Count);
            var categorias = (List<CategoriaDTO>)controller.ViewBag.Categorias;
            Assert.Single(categorias);
        }

        [Fact]
        public async Task CreateGet_Solicitante_RecebeApenasItensDoSeuSetor()
        {
            var controller = _fixture.CriarController(_fixture.Dados.Solicitante, ehTecnico: false);

            await controller.Create((int?)null);

            var itens = (List<ItemDTO>)controller.ViewBag.Itens;
            var item = Assert.Single(itens);
            Assert.Equal(_fixture.Dados.ItemDoSetorComum.Id, item.Id);
            Assert.Equal("Notebook Financeiro 01 - Notebook - Dell", item.Texto);
        }

        [Fact]
        public async Task CreateGet_PreencheOrigemIdRecebido()
        {
            var controller = _fixture.CriarController(_fixture.Dados.Solicitante, ehTecnico: false);

            await controller.Create((int?)42);

            Assert.Equal(42, (int?)controller.ViewBag.OrigemId);
        }

        // ---------- POST: validações ----------

        [Fact]
        public async Task CreatePost_UsuarioNaoCadastrado_RedirecionaParaLogin()
        {
            var fantasma = new Usuario { AspNetUserId = "nao-cadastrado", Nome = "Fantasma" };
            var controller = _fixture.CriarController(fantasma, ehTecnico: false);

            var result = await controller.Create(ModeloValido(_fixture.Dados.ItemDoSetorComum.Id), null);

            AssertRedirecionaPara(result, "Login", "Account");
            Assert.Empty(_fixture.Contexto.Tickets);
        }

        [Fact]
        public async Task CreatePost_UsuarioSemSetor_RedirecionaParaCompleteProfile()
        {
            var semSetor = SeedData.CriarUsuario(_fixture.Contexto, "sem-setor", "Sem Setor", setorId: null);
            var controller = _fixture.CriarController(semSetor, ehTecnico: false);

            var result = await controller.Create(ModeloValido(), null);

            AssertRedirecionaPara(result, "CompleteProfile", "Account");
            Assert.Empty(_fixture.Contexto.Tickets);
        }

        [Fact]
        public async Task CreatePost_ModelStateInvalido_RetornaViewComModeloERecarregaListas()
        {
            var controller = _fixture.CriarController(_fixture.Dados.Solicitante, ehTecnico: false);
            controller.ModelState.AddModelError("Titulo", "Obrigatório");
            var modelo = ModeloValido(_fixture.Dados.ItemDoSetorComum.Id);

            var result = await controller.Create(modelo, null);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(modelo, view.Model);
            Assert.NotNull(controller.ViewBag.Categorias);
            Assert.Single((List<ItemDTO>)controller.ViewBag.Itens);
            Assert.Empty(_fixture.Contexto.Tickets);
        }

        [Fact]
        public async Task CreatePost_SemPrioridadePadrao_RetornaViewComErro()
        {
            foreach (var prioridade in _fixture.Contexto.Prioridades)
            {
                prioridade.Padrao = false;
            }
            _fixture.Contexto.SaveChanges();
            var controller = _fixture.CriarController(_fixture.Dados.Solicitante, ehTecnico: false);

            var result = await controller.Create(ModeloValido(_fixture.Dados.ItemDoSetorComum.Id), null);

            Assert.IsType<ViewResult>(result);
            Assert.True(ContemErro(controller, "Error_SemPrioridade"));
            Assert.Empty(_fixture.Contexto.Tickets);
        }

        [Fact]
        public async Task CreatePost_SemStatusAberto_RetornaViewComErro()
        {
            _fixture.Contexto.Status.Remove(_fixture.Contexto.Status.Single(s => s.Codigo == (int)TicketStatus.Aberto));
            _fixture.Contexto.SaveChanges();
            var controller = _fixture.CriarController(_fixture.Dados.Solicitante, ehTecnico: false);

            var result = await controller.Create(ModeloValido(_fixture.Dados.ItemDoSetorComum.Id), null);

            Assert.IsType<ViewResult>(result);
            Assert.True(ContemErro(controller, "Error_SemStatusInicial"));
            Assert.Empty(_fixture.Contexto.Tickets);
        }

        // ---------- POST: sucesso ----------

        [Fact]
        public async Task CreatePost_Valido_CriaTicketEmAbertoComDadosDoUsuario()
        {
            var dados = _fixture.Dados;
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            var result = await controller.Create(ModeloValido(dados.ItemDoSetorComum.Id), null);

            AssertRedirecionaPara(result, "Index", "Tickets");
            var ticket = _fixture.Contexto.Tickets.Single();
            var statusAberto = _fixture.Contexto.Status.Single(s => s.Codigo == (int)TicketStatus.Aberto);
            Assert.Equal(dados.Solicitante.Id, ticket.UsuarioId);
            Assert.Equal(dados.SetorComum.Id, ticket.SetorId);
            Assert.Equal(dados.Categoria.Id, ticket.CategoriaId);
            Assert.Equal(dados.PrioridadePadrao.Id, ticket.PrioridadeId);
            Assert.Equal(statusAberto.Id, ticket.StatusId);
            Assert.Null(ticket.UsuarioTecnicoId);
            Assert.Null(ticket.TicketOrigemId);
            Assert.Null(ticket.DataFechamento);
            Assert.True(ticket.Ativo);
            Assert.Equal("Notebook não liga", ticket.Titulo);
            Assert.Equal(_fixture.Relogio.GetUtcNow().UtcDateTime, ticket.DataAbertura);
        }

        [Fact]
        public async Task CreatePost_Valido_CalculaPrazosPelaPrioridadePadrao()
        {
            var dados = _fixture.Dados;
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);
            var abertura = _fixture.Relogio.GetUtcNow().UtcDateTime;
            var horarios = _fixture.Contexto.Horarios.ToList();

            await controller.Create(ModeloValido(dados.ItemDoSetorComum.Id), null);

            var ticket = _fixture.Contexto.Tickets.Single();
            // Mesmo cálculo da controller: o teste garante que a prioridade padrão e o relógio são usados.
            Assert.Equal(
                SlaCalculator.CalcularPrazoSlaReal(new List<DateOnly>(), horarios, abertura, dados.PrioridadePadrao.TempoRespostaPadrao),
                ticket.PrazoRespostaSLA);
            Assert.Equal(
                SlaCalculator.CalcularPrazoSlaReal(new List<DateOnly>(), horarios, abertura, dados.PrioridadePadrao.TempoResolucaoPadrao),
                ticket.PrazoResolucaoSLA);
            Assert.True(ticket.PrazoResolucaoSLA > ticket.PrazoRespostaSLA);
        }

        [Fact]
        public async Task CreatePost_ComItens_VinculaItensAoTicket()
        {
            var dados = _fixture.Dados;
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            await controller.Create(ModeloValido(dados.ItemDoSetorComum.Id), null);

            var ticket = _fixture.Contexto.Tickets.Single();
            var vinculo = Assert.Single(_fixture.Contexto.ItensTickets.Where(it => it.TicketId == ticket.Id));
            Assert.Equal(dados.ItemDoSetorComum.Id, vinculo.ItemId);
            Assert.True(vinculo.Ativo);
        }

        [Fact]
        public async Task CreatePost_SemItens_NaoVinculaItens()
        {
            var controller = _fixture.CriarController(_fixture.Dados.Solicitante, ehTecnico: false);

            await controller.Create(ModeloValido(), null);

            Assert.Single(_fixture.Contexto.Tickets);
            Assert.Empty(_fixture.Contexto.ItensTickets);
        }

        [Fact]
        public async Task CreatePost_ComOrigemId_GravaTicketOrigemEAuditoriaDerivado()
        {
            var dados = _fixture.Dados;
            var origem = SeedData.CriarTicket(_fixture.Contexto, dados, TicketStatus.Resolvido);
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            await controller.Create(ModeloValido(), origem.Id);

            var novo = _fixture.Contexto.Tickets.Single(t => t.Id != origem.Id);
            Assert.Equal(origem.Id, novo.TicketOrigemId);
            var auditoria = _fixture.Contexto.AuditoriasTickets.Single(a => a.TicketId == novo.Id);
            var log = JsonSerializer.Deserialize<LogAuditoriaTicket>(auditoria.Log)!;
            Assert.Equal(origem.Id.ToString(), log.CamposModificados![TicketAcaoLog.Log_DerivadoDoTicket.ToString()]);
        }

        [Fact]
        public async Task CreatePost_Valido_GravaAuditoriaDeAberturaComPrazos()
        {
            var dados = _fixture.Dados;
            var controller = _fixture.CriarController(dados.Solicitante, ehTecnico: false);

            await controller.Create(ModeloValido(), null);

            var ticket = _fixture.Contexto.Tickets.Single();
            var auditoria = Assert.Single(_fixture.Contexto.AuditoriasTickets.Where(a => a.TicketId == ticket.Id));
            var log = JsonSerializer.Deserialize<LogAuditoriaTicket>(auditoria.Log)!;
            Assert.Equal(TicketAcaoLog.Log_AberturaChamado, log.Acao);
            Assert.Equal(dados.Solicitante.Nome, log.Usuario);
            Assert.Equal(_fixture.Relogio.GetUtcNow().UtcDateTime, auditoria.RegistradoEm);
            Assert.True(log.CamposModificados!.ContainsKey(TicketAcaoLog.Log_StatusInicial.ToString()));
            Assert.True(log.CamposModificados.ContainsKey(TicketAcaoLog.Log_PrazoResposta.ToString()));
            Assert.True(log.CamposModificados.ContainsKey(TicketAcaoLog.Log_PrazoResolucao.ToString()));
        }
    }
}
