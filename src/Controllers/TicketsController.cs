using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using ChamadosTi.Services.Interfaces;
using ChamadosTi.Utils;
using ChamadosTi.ViewModels.Categoria;
using ChamadosTi.ViewModels.Item;
using ChamadosTi.ViewModels.Ticket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Text.Json;

namespace ChamadosTi.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly ITicketService _ticketService;
        private readonly IStringLocalizer<TicketsController> _localizer;
        private readonly IUsuarioService _usuarioService;
        private readonly IPrioridadeService _prioridadeService;
        private readonly ICategoriaService _categoriaService;
        private readonly IItemService _itemService;
        private readonly TimeProvider _timeProvider;

        public TicketsController(ITicketService ticketService, IStringLocalizer<TicketsController> localizer, IUsuarioService usuarioService, IPrioridadeService prioridadeService, ICategoriaService categoriaService, IItemService itemService, TimeProvider timeProvider)
        {
            _ticketService = ticketService;
            _localizer = localizer;
            _usuarioService = usuarioService;
            _prioridadeService = prioridadeService;
            _categoriaService = categoriaService;
            _itemService = itemService;
            _timeProvider = timeProvider;
        }

        public async Task<IActionResult> Index(bool exibirTodos = false)
        {
            var usuarioLogado = await _usuarioService.ObterLogadoAsync(User);
            if (usuarioLogado is null)
            {
                return RedirectToAction("Login", "Account");
            }

            bool ehTecnico = _usuarioService.EhTecnico(User);
            bool verTodos = ehTecnico && exibirTodos;

            var tickets = await _ticketService.ListarParaUsuarioAsync(usuarioLogado.Id, ehTecnico, verTodos);

            ViewBag.EhTecnico = ehTecnico;
            ViewBag.ExibirTodos = verTodos;

            return View(tickets);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? origemId)
        {
            var usuarioLogado = await _usuarioService.ObterLogadoAsync(User);
            if (usuarioLogado is null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Categorias = await GetListaCategoriasAsync();
            ViewBag.Itens = _usuarioService.EhTecnico(User) ? await GetListaItensAsync() : await GetListaItensAsync(usuarioLogado.SetorId);
            ViewBag.OrigemId = origemId;

            return View(new TicketCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TicketCreateViewModel model, int? origemId)
        {
            var usuarioLogado = await _usuarioService.ObterLogadoAsync(User);
            if (usuarioLogado is null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (usuarioLogado.SetorId == null)
            {
                return RedirectToAction("CompleteProfile", "Account");
            }

            var ehTecnico = _usuarioService.EhTecnico(User);

            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = await GetListaCategoriasAsync();
                ViewBag.Itens = ehTecnico ? await GetListaItensAsync() : await GetListaItensAsync(usuarioLogado.SetorId);
                return View(model);
            }

            var prioridadePadrao = await GetPrioridadePadraoAsync();
            if (prioridadePadrao == null)
            {
                ModelState.AddModelError("", _localizer["Error_SemPrioridade"]);
                ViewBag.Categorias = await GetListaCategoriasAsync();
                ViewBag.Itens = ehTecnico ? await GetListaItensAsync() : await GetListaItensAsync(usuarioLogado.SetorId);
                return View(model);
            }

            var statusInicial = await GetStatusAsync((int)TicketStatus.Aberto);
            if (statusInicial == null)
            {
                ModelState.AddModelError("", _localizer["Error_SemStatusInicial"]);
                ViewBag.Categorias = await GetListaCategoriasAsync();
                ViewBag.Itens = ehTecnico ? await GetListaItensAsync() : await GetListaItensAsync(usuarioLogado.SetorId);
                return View(model);
            }

            var dataAberturaUtc = _timeProvider.GetUtcNow().UtcDateTime;

            var (prazoRespostaUtc, prazoResolucaoUtc) = await _ticketService.CalcularPrazosSlaAsync(dataAberturaUtc, usuarioLogado.SetorId, prioridadePadrao.TempoRespostaPadrao, prioridadePadrao.TempoResolucaoPadrao);

            var novoTicket = new Ticket
            {
                UsuarioId = usuarioLogado.Id,
                SetorId = usuarioLogado.SetorId.Value,
                CategoriaId = model.CategoriaId!.Value,
                PrioridadeId = prioridadePadrao.Id,
                StatusId = statusInicial.Id,
                DataAbertura = dataAberturaUtc,
                PrazoRespostaSLA = prazoRespostaUtc,
                PrazoResolucaoSLA = prazoResolucaoUtc,
                Titulo = model.Titulo,
                Comentario = model.Comentario,
                Ativo = true,
                TicketOrigemId = origemId
            };

            var dicionarioCampos = new Dictionary<string, string>
            {
                { TicketAcaoLog.Log_StatusInicial.ToString(), TicketAcaoLog.Log_StatusInicial.ToString() },
                { TicketAcaoLog.Log_PrazoResposta.ToString(), novoTicket.PrazoRespostaSLA.ToString("g") },
                { TicketAcaoLog.Log_PrazoResolucao.ToString(), novoTicket.PrazoResolucaoSLA.ToString("g") }
            };

            if (origemId is int idValido)
            {
                dicionarioCampos.Add(TicketAcaoLog.Log_DerivadoDoTicket.ToString(), idValido.ToString());
            }

            await _ticketService.CriarAsync(novoTicket, model.ItensIdsSelecionados, usuarioLogado.Nome, dicionarioCampos);

            return RedirectToAction("Index", "Tickets");
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var usuarioLogado = await _usuarioService.ObterLogadoAsync(User);
            if (usuarioLogado is null)
            {
                return RedirectToAction("Login", "Account");
            }

            var ticket = await _ticketService.ObterDetalhesAsync(id);

            bool ehTecnico = _usuarioService.EhTecnico(User);

            if (ticket == null || (!ehTecnico && ticket.UsuarioId != usuarioLogado.Id) )
            {
                return NotFound();
            }

            if (ticket.Status.Codigo > (int)TicketStatus.Aberto)
            {
                ehTecnico = ehTecnico && ticket.UsuarioTecnicoId == usuarioLogado.Id;
            }

            var itensIdsVinculados = ticket.ItensTickets?.Select(it => it.ItemId).ToList() ?? new List<int>();
            var historicoDTO = new List<TicketHistoricoAtivoDTO>();

            if (itensIdsVinculados.Any())
            {
                var chamadosAnteriores = await _ticketService.ObterChamadosAnterioresAsync(ticket.Id, itensIdsVinculados, 10);

                foreach (var hTicket in chamadosAnteriores)
                {
                    string parecer = string.Empty;
                    if (hTicket.AuditoriaTickets != null && hTicket.AuditoriaTickets.Any())
                    {
                        var ultimaAuditoria = hTicket.AuditoriaTickets
                            .OrderByDescending(a => a.RegistradoEm)
                            .FirstOrDefault(a => a.Log.Contains("Status_Resolvido") || a.Log.Contains("Status_Cancelado"));

                        if (ultimaAuditoria != null && !string.IsNullOrEmpty(ultimaAuditoria.Log))
                        {
                            try
                            {
                                var logDecodificado = JsonSerializer.Deserialize<LogAuditoriaTicket>(ultimaAuditoria.Log);
                                parecer = logDecodificado?.Mensagem ?? string.Empty;
                            }
                            catch { }
                        }
                    }

                    historicoDTO.Add(new TicketHistoricoAtivoDTO
                    {
                        Id = hTicket.Id,
                        Titulo = hTicket.Titulo,
                        DataAbertura = hTicket.DataAbertura.ToLocalTime().ToString("d"),
                        StatusNome = hTicket.Status.Nome,
                        ParecerTecnico = !string.IsNullOrEmpty(parecer) ? parecer : null,
                        ItemIds = hTicket.ItensTickets?.Select(it => it.ItemId).ToList() ?? new List<int>() // Mapeia os IDs dos ativos
                    });
                }
            }

            var auditoriasOriginais = await GetAuditoriasTicketAsync(ticket.Id);
            ViewBag.AuditoriasOriginais = auditoriasOriginais;
            ViewBag.LogsHistorico = DecodificarLogsAuditoria(auditoriasOriginais);
            ViewBag.Prioridades = await _prioridadeService.ListarOrdenadasPorIdAsync();
            ViewBag.Itens = ehTecnico ? await GetListaItensAsync() : await GetListaItensAsync(ticket.SetorId);
            ViewBag.EhTecnico = ehTecnico;
            ViewBag.HistoricoAtivo = historicoDTO;

            return View(ticket);
        }

        [Authorize(Roles = "Tecnico,Admin,AdminMaster")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assumir(int id)
        {
            var usuarioLogado = await _usuarioService.ObterLogadoAsync(User);
            if (usuarioLogado is null)
            {
                return RedirectToAction("Login", "Account");
            }

            var ticket = await GetTicketStatusAsync(id);
            bool tecnico = _usuarioService.EhTecnico(User);

            if (ticket == null || !tecnico)
            {
                return NotFound();
            }

            if (ticket.Status.Codigo != (int)TicketStatus.Aberto)
            {
                TempData["ErrorMessage"] = _localizer["Error_ChamadoJaAssumidoOuFechado"].Value;
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            var novoStatus = await GetStatusAsync((int)TicketStatus.EmAtendimento);
            if (novoStatus == null)
            {
                TempData["ErrorMessage"] = _localizer["Error_StatusAtendimentoNaoEncontrado"].Value;
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            ticket.UsuarioTecnicoId = usuarioLogado.Id;
            ticket.StatusId = novoStatus.Id;

            var camposModificados = new Dictionary<string, string>
            {
                { TicketAcaoLog.Log_StatusAlterado.ToString(), "Status_EmAtendimento" },
                { TicketAcaoLog.Log_TecnicoAtribuido.ToString(), usuarioLogado.Nome }
            };

            await _ticketService.RegistrarTransicaoAsync(ticket, TicketAcaoLog.Log_ChamadoAssumido, usuarioLogado.Nome, null, camposModificados);

            TempData["SuccessMessage"] = _localizer["Success_ChamadoAssumido"].Value;
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarComentario(int ticketId, string mensagemTexto)
        {
            var usuarioLogado = await _usuarioService.ObterLogadoAsync(User);
            if (usuarioLogado is null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(mensagemTexto))
            {
                TempData["ErrorMessage"] = _localizer["Error_ComentarioVazio"].Value;
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            var ticket = await _ticketService.ObterPorIdAsync(ticketId);
            bool tecnico = _usuarioService.EhTecnico(User);

            if (ticket == null || (tecnico && ticket.UsuarioTecnicoId != usuarioLogado.Id) || (!tecnico && ticket.UsuarioId != usuarioLogado.Id) )
            {
                return NotFound();
            }

            var dadosLog = new Dictionary<string, string> {
                { TicketAcaoLog.Log_NovaInteracao.ToString(), mensagemTexto.Trim() }
            };

            await _ticketService.RegistrarAuditoriaAsync(ticketId, TicketAcaoLog.Log_NovaInteracao, usuarioLogado.Nome, null, dadosLog);

            TempData["SuccessMessage"] = _localizer["Success_ComentarioAdicionado"].Value;
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        [Authorize(Roles = "Tecnico,Admin,AdminMaster")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoverParaPendente(int id)
        {
            var usuarioLogado = await _usuarioService.ObterLogadoAsync(User);
            if (usuarioLogado is null) return RedirectToAction("Login", "Account");

            var ticket = await GetTicketStatusAsync(id);

            bool tecnico = _usuarioService.EhTecnico(User);

            if (ticket == null || !tecnico || (tecnico && ticket.UsuarioTecnicoId != usuarioLogado.Id))
            {
                return NotFound();
            }

            if (ticket.Status.Codigo != (int)TicketStatus.EmAtendimento)
            {
                TempData["ErrorMessage"] = _localizer["Error_StatusInvalidoParaTramitar"].Value;
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            var statusPendente = await GetStatusAsync((int)TicketStatus.Pendente);
            if (statusPendente == null) return NotFound();

            ticket.StatusId = statusPendente.Id;

            await _ticketService.RegistrarTransicaoAsync(ticket, TicketAcaoLog.Log_ChamadoPendente, usuarioLogado.Nome, null, null);

            TempData["SuccessMessage"] = _localizer["Success_ChamadoPendente"].Value;
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [Authorize(Roles = "Tecnico,Admin,AdminMaster")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RetomarAtendimento(int id)
        {
            var usuarioLogado = await _usuarioService.ObterLogadoAsync(User);
            if (usuarioLogado is null) return RedirectToAction("Login", "Account");

            var ticket = await GetTicketStatusAsync(id);

            bool tecnico = _usuarioService.EhTecnico(User);

            if (ticket == null || !tecnico || (tecnico && ticket.UsuarioTecnicoId != usuarioLogado.Id))
            {
                return NotFound();
            }

            if (ticket.Status.Codigo != (int)TicketStatus.Pendente)
            {
                TempData["ErrorMessage"] = _localizer["Error_StatusInvalidoParaTramitar"].Value;
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            var statusAtendimento = await GetStatusAsync((int)TicketStatus.EmAtendimento);
            if (statusAtendimento == null) return NotFound();

            var dataRetomadaUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var ultimaAuditoriaPausa = await _ticketService.ObterUltimaAuditoriaPausaAsync(id);

            var dicionarioModificacoesLog = new Dictionary<string, string>
            {
                { TicketAcaoLog.Log_StatusAlterado.ToString(), "Status_EmAtendimento" }
            };

            if (ultimaAuditoriaPausa != null)
            {
                TimeSpan tempoCongelado = dataRetomadaUtc - ultimaAuditoriaPausa.RegistradoEm;
                double minutosParaEstender = tempoCongelado.TotalMinutes;

                if (minutosParaEstender > 0)
                {
                    ticket.PrazoResolucaoSLA = ticket.PrazoResolucaoSLA.AddMinutes(minutosParaEstender);
                    dicionarioModificacoesLog.Add(TicketAcaoLog.Log_PrazoResolucaoEstendido.ToString(), ticket.PrazoResolucaoSLA.ToString("o"));
                }
            }

            ticket.StatusId = statusAtendimento.Id;

            await _ticketService.RegistrarTransicaoAsync(ticket, TicketAcaoLog.Log_ChamadoRetomado, usuarioLogado.Nome, null, dicionarioModificacoesLog);

            TempData["SuccessMessage"] = _localizer["Success_ChamadoRetomado"].Value;
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [Authorize(Roles = "Tecnico,Admin,AdminMaster")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoverParaResolvido(int id, string justificativaTexto)
        {
            var usuarioLogado = await _usuarioService.ObterLogadoAsync(User);
            if (usuarioLogado is null) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(justificativaTexto))
            {
                TempData["ErrorMessage"] = _localizer["Error_ParecerObrigatorio"].Value;
                return RedirectToAction(nameof(Details), new { id = id });
            }

            var ticket = await GetTicketStatusAsync(id);
            bool tecnico = _usuarioService.EhTecnico(User);

            if (ticket == null || !tecnico || (tecnico && ticket.UsuarioTecnicoId != usuarioLogado.Id))
            {
                return NotFound();
            }

            if (ticket.Status.Codigo != (int)TicketStatus.EmAtendimento)
            {
                TempData["ErrorMessage"] = _localizer["Error_StatusInvalidoParaTramitar"].Value;
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            var statusResolvido = await GetStatusAsync((int)TicketStatus.Resolvido);
            if (statusResolvido == null) return NotFound();

            ticket.StatusId = statusResolvido.Id;
            ticket.DataFechamento = _timeProvider.GetUtcNow().UtcDateTime;

            var camposModificados = new Dictionary<string, string> {
                { TicketAcaoLog.Log_StatusAlterado.ToString(), "Status_Resolvido" }
            };

            await _ticketService.RegistrarTransicaoAsync(ticket, TicketAcaoLog.Log_ChamadoResolvido, usuarioLogado.Nome, justificativaTexto.Trim(), camposModificados);

            TempData["SuccessMessage"] = _localizer["Success_ChamadoResolvido"].Value;
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id, string justificativaTexto)
        {
            var usuarioLogado = await _usuarioService.ObterLogadoAsync(User);
            if (usuarioLogado is null) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(justificativaTexto))
            {
                TempData["ErrorMessage"] = _localizer["Error_JustificativaObrigatoria"].Value;
                return RedirectToAction(nameof(Details), new { id = id });
            }

            var ticket = await GetTicketStatusAsync(id);
            bool tecnico = _usuarioService.EhTecnico(User);

            if (ticket == null ||
                (tecnico && ticket.Status.Codigo > (int)TicketStatus.Aberto && ticket.UsuarioTecnicoId != usuarioLogado.Id) ||
                (!tecnico && (ticket.UsuarioId != usuarioLogado.Id || ticket.Status.Codigo != (int)TicketStatus.Aberto)))
            {
                return NotFound();
            }

            if (ticket.Status.Codigo >= (int)TicketStatus.Resolvido)
            {
                TempData["ErrorMessage"] = _localizer["Error_StatusInvalidoParaCancelar"].Value;
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            var statusCancelado = await GetStatusAsync((int)TicketStatus.Cancelado);
            if (statusCancelado == null) return NotFound();

            ticket.StatusId = statusCancelado.Id;
            ticket.DataFechamento = _timeProvider.GetUtcNow().UtcDateTime;

            var camposModificados = new Dictionary<string, string> {
                { TicketAcaoLog.Log_StatusAlterado.ToString(), "Status_Cancelado" }
            };

            await _ticketService.RegistrarTransicaoAsync(ticket, TicketAcaoLog.Log_ChamadoCancelado, usuarioLogado.Nome, justificativaTexto.Trim(), camposModificados);

            TempData["SuccessMessage"] = _localizer["Success_ChamadoCancelado"].Value;
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [Authorize(Roles = "Tecnico,Admin,AdminMaster")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarPrioridade(int ticketId, [FromForm(Name = "PrioridadeId")] int prioridadeId)
        {
            var usuarioLogado = await _usuarioService.ObterLogadoAsync(User);
            if (usuarioLogado is null) return RedirectToAction("Login", "Account");

            var ticket = await GetTicketStatusAsync(ticketId);
            bool tecnico = _usuarioService.EhTecnico(User);

            if (ticket == null || (ticket.Status.Codigo > (int)TicketStatus.Aberto && tecnico && ticket.UsuarioTecnicoId != usuarioLogado.Id))
            {
                return NotFound();
            }

            if (ticket!.Status.Codigo >= (int)TicketStatus.Resolvido)
            {
                TempData["ErrorMessage"] = _localizer["Error_StatusInvalidoParaAlterarPrioridade"].Value;
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            var prioridade = await GetPrioridadeAsync(prioridadeId);
            if (prioridade == null) return NotFound();

            var (prazoRespostaUtc, prazoResolucaoUtc) = await _ticketService.CalcularPrazosSlaAsync(ticket.DataAbertura, usuarioLogado.SetorId, prioridade.TempoRespostaPadrao, prioridade.TempoResolucaoPadrao);

            ticket.PrioridadeId = prioridade.Id;
            ticket.PrazoResolucaoSLA = prazoResolucaoUtc;

            var camposModificados = new Dictionary<string, string>();

            if (ticket.Status.Codigo == (int)TicketStatus.Aberto)
            {
                camposModificados.Add(TicketAcaoLog.Log_PrazoResposta.ToString(), prazoRespostaUtc.ToString("g"));
                ticket.PrazoRespostaSLA = prazoRespostaUtc;
            }

            camposModificados.Add(TicketAcaoLog.Log_PrazoResolucao.ToString(), prazoResolucaoUtc.ToString("g"));
            camposModificados.Add(TicketAcaoLog.Log_PrioridadeAlterada.ToString(), prioridade.Nome);

            await _ticketService.RegistrarTransicaoAsync(ticket, TicketAcaoLog.Log_PrioridadeAlterada, usuarioLogado.Nome, null, camposModificados);

            TempData["SuccessMessage"] = _localizer["Success_PrioridadeAlterada"].Value;
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [Authorize(Roles = "Tecnico,Admin,AdminMaster")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarItens(int ticketId, List<int> itensIdsSelecionados)
        {
            var usuarioLogado = await _usuarioService.ObterLogadoAsync(User);
            if (usuarioLogado is null) return RedirectToAction("Login", "Account");

            var ticket = await GetTicketStatusAsync(ticketId);
            bool tecnico = _usuarioService.EhTecnico(User);

            if (ticket == null || (ticket.Status.Codigo > (int)TicketStatus.Aberto && tecnico && ticket.UsuarioTecnicoId != usuarioLogado.Id))
            {
                return NotFound();
            }

            if (ticket.Status.Codigo >= (int)TicketStatus.Resolvido)
            {
                TempData["ErrorMessage"] = _localizer["Error_StatusInvalidoParaAlterarItens"].Value;
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            var camposModificados = new Dictionary<string, string>
            {
                { TicketAcaoLog.Log_ItensAlterados.ToString(), itensIdsSelecionados != null && itensIdsSelecionados.Any() ? string.Join(", ", itensIdsSelecionados) : "Nenhum" }
            };

            await _ticketService.AlterarItensAsync(ticketId, itensIdsSelecionados, usuarioLogado.Nome, camposModificados);

            TempData["SuccessMessage"] = _localizer["Success_ItensAlterados"].Value;
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        private async Task<Status?> GetStatusAsync(int codigoStatus)
        {
            return await _ticketService.ObterStatusPorCodigoAsync(codigoStatus);
        }

        private async Task<Prioridade?> GetPrioridadePadraoAsync()
        {
            return await _prioridadeService.ObterPadraoAsync();
        }

        private async Task<Prioridade?> GetPrioridadeAsync(int prioridadeId)
        {
            return await _prioridadeService.ObterPorIdAsync(prioridadeId);
        }

        private async Task<List<ItemDTO>> GetListaItensAsync(int? setorId = null)
        {
            var itens = await _itemService.ListarParaSelecaoAsync(setorId);

            return itens.Select(item => new ItemDTO
            {
                Id = item.Id,
                Texto = $"{item.Nome} - {item.Tipos.Nome} - {item.Marcas.Nome}"
            }).ToList();
        }

        private async Task<List<CategoriaDTO>> GetListaCategoriasAsync()
        {
            var categorias = await _categoriaService.ListarOrdenadosPorNomeAsync();

            return categorias.Select(categoria => new CategoriaDTO
            {
                Id = categoria.Id,
                Nome = categoria.Nome
            }).ToList();
        }

        private async Task<List<AuditoriaTicket>> GetAuditoriasTicketAsync(int ticketId)
        {
            return await _ticketService.ObterAuditoriasAsync(ticketId);
        }

        private static List<LogAuditoriaTicket> DecodificarLogsAuditoria(List<AuditoriaTicket> auditoriasTicket)
        {
            var listaLogsDecodificados = new List<LogAuditoriaTicket>();

            foreach (var auditoria in auditoriasTicket)
            {
                try
                {
                    if (!string.IsNullOrEmpty(auditoria.Log))
                    {
                        var logEstruturado = JsonSerializer.Deserialize<LogAuditoriaTicket>(auditoria.Log);
                        if (logEstruturado != null)
                        {
                            listaLogsDecodificados.Add(logEstruturado);
                        }
                    }
                }
                catch (JsonException)
                {
                    listaLogsDecodificados.Add(new LogAuditoriaTicket
                    {
                        Acao = TicketAcaoLog.Log_ErrorCorrompido,
                        Usuario = "System"
                    });
                }
            }
            return listaLogsDecodificados;
        }

        private async Task<Ticket?> GetTicketStatusAsync(int id)
        {
            return await _ticketService.ObterComStatusAsync(id);
        }
    }
}
