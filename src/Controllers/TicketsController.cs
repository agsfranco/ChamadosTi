using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using ChamadosTi.Services.Interfaces;
using ChamadosTi.Utils;
using ChamadosTi.ViewModels.Categoria;
using ChamadosTi.ViewModels.Ticket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Text.Json;

namespace ChamadosTi.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IStringLocalizer<TicketsController> _localizer;
        private readonly IUsuarioService _usuarioService;

        public TicketsController(AppDbContext context, UserManager<IdentityUser> userManager, IStringLocalizer<TicketsController> localizer, IUsuarioService usuarioService)
        {
            _context = context;
            _userManager = userManager;
            _localizer = localizer;
            _usuarioService = usuarioService;
        }

        public async Task<IActionResult> Index()
        {
            var tickets = await _context.Tickets
                                .Include(t => t.Status)
                                .Include(t => t.Prioridades)
                                .Include(t => t.Categorias)
                                .Include(t => t.Usuarios) 
                                .Include(t => t.UsuariosTecnicos) 
                                .OrderByDescending(t => t.DataAbertura) 
                                .ToListAsync();

            return View(tickets);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? origemId)
        {
            var usuarioLogado = await GetUsuarioAsync();
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
            var usuarioLogado = await GetUsuarioAsync();
            if (usuarioLogado is null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (usuarioLogado.SetorId == null) 
            {
                return RedirectToAction("CompleteProfile", "Account");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = await GetListaCategoriasAsync();
                ViewBag.Itens = _usuarioService.EhTecnico(User) ? await GetListaItensAsync() : await GetListaItensAsync(usuarioLogado.SetorId);
                return View(model);
            }

            var prioridadePadrao = await GetPrioridadePadraoAsync();
            if (prioridadePadrao == null)
            {
                ModelState.AddModelError("", _localizer["Error_SemPrioridade"]);
                ViewBag.Categorias = await GetListaCategoriasAsync();
                ViewBag.Itens = _usuarioService.EhTecnico(User) ? await GetListaItensAsync() : await GetListaItensAsync(usuarioLogado.SetorId);
                return View(model);
            }

            var statusInicial = await GetStatusAsync((int)TicketStatus.Aberto);
            if (statusInicial == null)
            {
                ModelState.AddModelError("", _localizer["Error_SemStatusInicial"]);
                ViewBag.Categorias = await GetListaCategoriasAsync();
                ViewBag.Itens = _usuarioService.EhTecnico(User) ? await GetListaItensAsync() : await GetListaItensAsync(usuarioLogado.SetorId);
                return View(model);
            }

            var dataAberturaUtc = DateTime.UtcNow;
            var todosFeriados = await GetFeriadosAsync(DateOnly.FromDateTime(dataAberturaUtc));
            var horariosSetores = await GetHorariosSetoresAsync(usuarioLogado);

            var prazoRespostaUtc = SlaCalculator.CalcularPrazoSlaReal(todosFeriados, horariosSetores, dataAberturaUtc, prioridadePadrao.TempoRespostaPadrao);
            var prazoResolucaoUtc = SlaCalculator.CalcularPrazoSlaReal(todosFeriados, horariosSetores, dataAberturaUtc, prioridadePadrao.TempoResolucaoPadrao);

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
        
            _context.Tickets.Add(novoTicket);
            await _context.SaveChangesAsync();

            if (model.ItensIdsSelecionados != null && model.ItensIdsSelecionados.Any())
            {
                foreach (var itemId in model.ItensIdsSelecionados)
                {
                    _context.ItensTickets.Add(new ItemTicket
                    {
                        TicketId = novoTicket.Id,
                        ItemId = itemId,
                        Ativo = true
                    });
                }
            }                

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
            
            var auditoriaTicket = MontarAuditoriaTicket(TicketAcaoLog.Log_AberturaChamado, usuarioLogado.Nome, novoTicket.Id, null, dicionarioCampos);
            _context.AuditoriasTickets.Add(auditoriaTicket);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Tickets");
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var usuarioSistema = await GetUsuarioAsync();
            if (usuarioSistema is null)
            {
                return RedirectToAction("Login", "Account");
            }

            var ticket = await _context.Tickets
                                    .Include(t => t.Status)
                                    .Include(t => t.Prioridades)
                                    .Include(t => t.Categorias)
                                    .Include(t => t.Usuarios) 
                                    .Include(t => t.UsuariosTecnicos) 
                                    .Include(t => t.Setores) 
                                    .Include(t => t.ItensTickets!)
                                    .ThenInclude(it => it.Item)
                                    .ThenInclude(i => i.Marcas)
                                    .Include(t => t.ItensTickets!)
                                    .ThenInclude(it => it.Item)
                                    .ThenInclude(i => i.Tipos)
                                    .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }
    
            var itensIdsVinculados = ticket.ItensTickets?.Select(it => it.ItemId).ToList() ?? new List<int>();
            var historicoDTO = new List<TicketHistoricoAtivoDTO>();

            if (itensIdsVinculados.Any())
            {
                var chamadosAnteriores = await _context.Tickets
                    .Include(t => t.Status)
                    .Include(t => t.ItensTickets)
                    .Include(t => t.AuditoriaTickets)
                    .Where(t => t.Id != ticket.Id && t.ItensTickets!.Any(it => itensIdsVinculados.Contains(it.ItemId)))
                    .OrderByDescending(t => t.DataAbertura)
                    .Take(10) //limita em 10
                    .ToListAsync();

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
                                var logDecodificado = System.Text.Json.JsonSerializer.Deserialize<LogAuditoriaTicket>(ultimaAuditoria.Log);
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

            bool ehTecnico = _usuarioService.EhTecnico(User);

            var auditoriasOriginais = await GetAuditoriasTicketAsync(ticket.Id);
            ViewBag.AuditoriasOriginais = auditoriasOriginais;
            ViewBag.LogsHistorico = await GetLogsAuditoriaAsync(ticket.Id, auditoriasOriginais);
            ViewBag.Prioridades = await GetListaPrioridadesAsync(); 
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
            var tecnicoSistema = await GetUsuarioAsync();
            if (tecnicoSistema is null)
            {
                return RedirectToAction("Login", "Account");
            }

            var ticket = await GetTicketStatusAsync(id);
            if (ticket == null)
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

            ticket.UsuarioTecnicoId = tecnicoSistema.Id;
            ticket.StatusId = novoStatus.Id;
            _context.Tickets.Update(ticket);

            var camposModificados = new Dictionary<string, string>
            {
                { TicketAcaoLog.Log_StatusAlterado.ToString(), "Status_EmAtendimento" },
                { TicketAcaoLog.Log_TecnicoAtribuido.ToString(), tecnicoSistema.Nome }
            };

            var auditoriaTicket = MontarAuditoriaTicket(TicketAcaoLog.Log_ChamadoAssumido, tecnicoSistema.Nome, ticket.Id, null, camposModificados);
            _context.AuditoriasTickets.Add(auditoriaTicket);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Success_ChamadoAssumido"].Value;
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarComentario(int ticketId, string mensagemTexto)
        {
            var usuarioLogado = await GetUsuarioAsync();
            if (usuarioLogado is null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(mensagemTexto))
            {
                TempData["ErrorMessage"] = _localizer["Error_ComentarioVazio"].Value;
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            var ticketExiste = await _context.Tickets.AnyAsync(t => t.Id == ticketId);
            if (!ticketExiste)
            {
                return NotFound();
            }

            var dadosLog = new Dictionary<string, string> {
                { TicketAcaoLog.Log_NovaInteracao.ToString(), mensagemTexto.Trim() }
            };

            var auditoriaTicket = MontarAuditoriaTicket(TicketAcaoLog.Log_NovaInteracao, usuarioLogado.Nome, ticketId, null, dadosLog);
            _context.AuditoriasTickets.Add(auditoriaTicket);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Success_ComentarioAdicionado"].Value;
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        [Authorize(Roles = "Tecnico,Admin,AdminMaster")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoverParaPendente(int id)
        {
            var tecnicoSistema = await GetUsuarioAsync();
            if (tecnicoSistema is null) return RedirectToAction("Login", "Account");

            var ticket = await GetTicketStatusAsync(id);
            if (ticket == null) return NotFound();

            if (ticket.Status.Codigo != (int)TicketStatus.EmAtendimento)
            {
                TempData["ErrorMessage"] = _localizer["Error_StatusInvalidoParaTramitar"].Value;
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            var statusPendente = await GetStatusAsync((int)TicketStatus.Pendente);
            if (statusPendente == null) return NotFound();

            ticket.StatusId = statusPendente.Id;
            _context.Tickets.Update(ticket);

            _context.AuditoriasTickets.Add(MontarAuditoriaTicket(TicketAcaoLog.Log_ChamadoPendente, tecnicoSistema.Nome, ticket.Id));
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Success_ChamadoPendente"].Value;
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [Authorize(Roles = "Tecnico,Admin,AdminMaster")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RetomarAtendimento(int id)
        {
            var tecnicoSistema = await GetUsuarioAsync();
            if (tecnicoSistema is null) return RedirectToAction("Login", "Account");

            var ticket = await GetTicketStatusAsync(id);
            if (ticket == null) return NotFound();

            if (ticket.Status.Codigo != (int)TicketStatus.Pendente)
            {
                TempData["ErrorMessage"] = _localizer["Error_StatusInvalidoParaTramitar"].Value;
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            var statusAtendimento = await GetStatusAsync((int)TicketStatus.EmAtendimento);
            if (statusAtendimento == null) return NotFound();

            var dataRetomadaUtc = DateTime.UtcNow;
            var ultimaAuditoriaPausa = await _context.AuditoriasTickets
                                                     .Where(a => a.TicketId == id && a.Log.Contains("Status_Pendente"))
                                                     .OrderByDescending(a => a.RegistradoEm)
                                                     .FirstOrDefaultAsync();

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
            _context.Tickets.Update(ticket);

            _context.AuditoriasTickets.Add(MontarAuditoriaTicket(TicketAcaoLog.Log_ChamadoRetomado, tecnicoSistema.Nome, ticket.Id, null, dicionarioModificacoesLog));
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Success_ChamadoRetomado"].Value;
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [Authorize(Roles = "Tecnico,Admin,AdminMaster")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoverParaResolvido(int id, string justificativaTexto)
        {
            var tecnicoSistema = await GetUsuarioAsync();
            if (tecnicoSistema is null) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(justificativaTexto))
            {
                TempData["ErrorMessage"] = _localizer["Error_ParecerObrigatorio"].Value;
                return RedirectToAction(nameof(Details), new { id = id });
            }

            var ticket = await GetTicketStatusAsync(id);
            if (ticket == null) return NotFound();

            if (ticket.Status.Codigo != (int)TicketStatus.EmAtendimento && ticket.Status.Codigo != (int)TicketStatus.Pendente)
            {
                TempData["ErrorMessage"] = _localizer["Error_StatusInvalidoParaTramitar"].Value;
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            var statusResolvido = await GetStatusAsync((int)TicketStatus.Resolvido);
            if (statusResolvido == null) return NotFound();

            ticket.StatusId = statusResolvido.Id;
            ticket.DataFechamento = DateTime.UtcNow; 
            _context.Tickets.Update(ticket);

            var camposModificados = new Dictionary<string, string> { 
                { TicketAcaoLog.Log_StatusAlterado.ToString(), "Status_Resolvido" } 
            };

            _context.AuditoriasTickets.Add(MontarAuditoriaTicket(TicketAcaoLog.Log_ChamadoResolvido, tecnicoSistema.Nome, ticket.Id, justificativaTexto.Trim(), camposModificados));
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = _localizer["Success_ChamadoResolvido"].Value;
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [Authorize(Roles = "Tecnico,Admin,AdminMaster")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id, string justificativaTexto)
        {
            var usuarioLogado = await GetUsuarioAsync();
            if (usuarioLogado is null) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(justificativaTexto))
            {
                TempData["ErrorMessage"] = _localizer["Error_JustificativaObrigatoria"].Value;
                return RedirectToAction(nameof(Details), new { id = id });
            }

            var ticket = await GetTicketStatusAsync(id);
            if (ticket == null) return NotFound();

            if (ticket.Status.Codigo >= (int)TicketStatus.Resolvido)
            {
                TempData["ErrorMessage"] = _localizer["Error_StatusInvalidoParaCancelar"].Value;
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            var statusCancelado = await GetStatusAsync((int)TicketStatus.Cancelado);
            if (statusCancelado == null) return NotFound();

            ticket.StatusId = statusCancelado.Id;
            ticket.DataFechamento = DateTime.UtcNow; 
            _context.Tickets.Update(ticket);

            var camposModificados = new Dictionary<string, string> { 
                { TicketAcaoLog.Log_StatusAlterado.ToString(), "Status_Cancelado" }
            };

            _context.AuditoriasTickets.Add(MontarAuditoriaTicket(TicketAcaoLog.Log_ChamadoCancelado, usuarioLogado.Nome, ticket.Id, justificativaTexto.Trim(), camposModificados));
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Success_ChamadoCancelado"].Value;
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [Authorize(Roles = "Tecnico,Admin,AdminMaster")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarPrioridade(int ticketId, [FromForm(Name = "PrioridadeId")] int prioridadeId)
        {
            var usuarioLogado = await GetUsuarioAsync();
            if (usuarioLogado is null) return RedirectToAction("Login", "Account");

            var ticket = await GetTicketStatusAsync(ticketId);
            if (ticket == null) return NotFound();

            if (ticket.Status.Codigo >= (int)TicketStatus.Resolvido)
            {
                TempData["ErrorMessage"] = _localizer["Error_StatusInvalidoParaAlterarPrioridade"].Value;
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            var prioridade = await GetPrioridadeAsync(prioridadeId);
            if (prioridade == null) return NotFound();

            var feriados = await GetFeriadosAsync(DateOnly.FromDateTime(ticket.DataAbertura));
            var horariosSetores = await GetHorariosSetoresAsync(usuarioLogado);

            var prazoResolucaoUtc = SlaCalculator.CalcularPrazoSlaReal(feriados, horariosSetores, ticket.DataAbertura, prioridade.TempoResolucaoPadrao);

            ticket.PrioridadeId = prioridade.Id;
            ticket.PrazoResolucaoSLA = prazoResolucaoUtc;

            var camposModificados = new Dictionary<string, string>();

            if (ticket.Status.Codigo == (int)TicketStatus.Aberto)
            {
                var prazoRespostaUtc = SlaCalculator.CalcularPrazoSlaReal(feriados, horariosSetores, ticket.DataAbertura, prioridade.TempoRespostaPadrao);
                camposModificados.Add(TicketAcaoLog.Log_PrazoResposta.ToString(), prazoRespostaUtc.ToString("g"));
                ticket.PrazoRespostaSLA = prazoRespostaUtc;
            }

            camposModificados.Add(TicketAcaoLog.Log_PrazoResolucao.ToString(), prazoResolucaoUtc.ToString("g"));
            camposModificados.Add(TicketAcaoLog.Log_PrioridadeAlterada.ToString(), prioridade.Nome);            

            _context.Tickets.Update(ticket);
            _context.AuditoriasTickets.Add(MontarAuditoriaTicket(TicketAcaoLog.Log_PrioridadeAlterada, usuarioLogado.Nome, ticket.Id, null, camposModificados));
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = _localizer["Success_PrioridadeAlterada"].Value;
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [Authorize(Roles = "Tecnico,Admin,AdminMaster")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarItens(int ticketId, List<int> itensIdsSelecionados)
        {
            var usuarioLogado = await GetUsuarioAsync();
            if (usuarioLogado is null) return RedirectToAction("Login", "Account");

            var ticket = await GetTicketStatusAsync(ticketId);
            if (ticket == null) return NotFound();

            if (ticket.Status.Codigo >= (int)TicketStatus.Resolvido)
            {
                TempData["ErrorMessage"] = _localizer["Error_StatusInvalidoParaAlterarItens"].Value;
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            await _context.ItensTickets
                          .Where(it => it.TicketId == ticketId)
                          .ExecuteDeleteAsync();

            if (itensIdsSelecionados != null && itensIdsSelecionados.Any())
            {
                foreach (var itemId in itensIdsSelecionados)
                {
                    _context.ItensTickets.Add(new ItemTicket
                    {
                        TicketId = ticketId,
                        ItemId = itemId,
                        Ativo = true
                    });
                }
            }

            var camposModificados = new Dictionary<string, string>
            {
                { TicketAcaoLog.Log_ItensAlterados.ToString(), itensIdsSelecionados != null ? string.Join(", ", itensIdsSelecionados) : "Nenhum" }
            };

            _context.AuditoriasTickets.Add(MontarAuditoriaTicket(TicketAcaoLog.Log_ItensAlterados, usuarioLogado.Nome, ticket.Id, null, camposModificados));
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Success_ItensAlterados"].Value;
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        private async Task<Usuario?> GetUsuarioAsync()
        {
            var identityUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(identityUserId))
            {
                return null;
            }

            return await _usuarioService.GetUsuarioLocalPorAspNetIdAsync(identityUserId);
        }

        private async Task<Status?> GetStatusAsync(int codigoStatus)
        {
            return await _context.Status.FirstOrDefaultAsync(s => s.Codigo == codigoStatus);
        }

        private async Task<Prioridade?> GetPrioridadePadraoAsync()
        {
            return await _context.Prioridades.FirstOrDefaultAsync(p => p.Padrao == true);
        }

        private async Task<Prioridade?> GetPrioridadeAsync(int prioridadeId)
        {
            return await _context.Prioridades.FirstOrDefaultAsync(p => p.Id == prioridadeId);
        }

        private async Task<dynamic?> GetListaItensAsync(int? setorId = null)
        {
            var query = _context.Itens
                                .Include(i => i.Tipos)
                                .Include(i => i.Marcas)
                                .Include(i => i.Setores)
                                .AsQueryable();
            if (setorId != null)
            {
                query = query.Where(i => i.SetorId == setorId);
            }
            return await query.OrderBy(i => i.Nome)
                              .ThenBy(i => i.Tipos.Nome)
                              .ThenBy(i => i.Marcas.Nome)
                              .Select(i => new { Id = i.Id, Texto = i.Nome + " - " + i.Tipos.Nome + " - " + i.Marcas.Nome })
                              .ToListAsync();
        }

        private async Task<List<CategoriaDTO>> GetListaCategoriasAsync()
        {
            var categorias = await _context.Categorias.OrderBy(c => c.Nome).ToListAsync();
            var categoriaDTO = new List<CategoriaDTO>();
            foreach (Categoria categoria in categorias)
            {
                categoriaDTO.Add(new CategoriaDTO { 
                    Id = categoria.Id,
                    Nome = categoria.Nome 
                });
            }
            return categoriaDTO;
        }

        private async Task<List<AuditoriaTicket>> GetAuditoriasTicketAsync(int ticketId)
        {
            return await _context.AuditoriasTickets
                                 .Where(a => a.TicketId == ticketId)
                                 .OrderBy(a => a.RegistradoEm)
                                 .ToListAsync();
        }
        
        private async Task<List<LogAuditoriaTicket>> GetLogsAuditoriaAsync(int ticketId, List<AuditoriaTicket>? auditoriasTicket)
        {
            if (auditoriasTicket is null)
            {
               auditoriasTicket = await GetAuditoriasTicketAsync(ticketId);
            }

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
            return await _context.Tickets.Include(t => t.Status).FirstOrDefaultAsync(t => t.Id == id);
        }

        private async Task<List<Prioridade>> GetListaPrioridadesAsync()
        {
            return await _context.Prioridades.OrderBy(p => p.Id).ToListAsync();
        }

        private async Task<List<Horario>> GetHorariosSetoresAsync(Usuario usuarioLogado)
        {
            var setorTiDoSistema = await _context.Setores.FirstOrDefaultAsync(s => s.Ti == true);
            var setorSlaId = setorTiDoSistema != null ? setorTiDoSistema.Id : usuarioLogado.SetorId;

            return await _context.SetoresHorarios
                         .Include(s => s.Horario)
                         .Where(s => s.SetorId == setorSlaId)
                         .Select(s => s.Horario)
                         .ToListAsync();
        }

        private async Task<List<DateOnly>> GetFeriadosAsync(DateOnly dataUtc)
        {
            return await _context.Feriados
                         .Where(f => f.Dia >= dataUtc)
                         .Select(f => f.Dia)
                         .ToListAsync();
        }

        private async Task<Setor?> GetSetorTiAsync()
        {
            return await _context.Setores.FirstOrDefaultAsync(s => s.Ti == true);
        }

        private AuditoriaTicket MontarAuditoriaTicket(TicketAcaoLog acao, string usuarioLogadoNome, int ticketId, string? message = null, Dictionary<string, string>? dicionarioCampos = null)
        {
            var dadosLog = new LogAuditoriaTicket
            {
                Acao = acao,
                Usuario = usuarioLogadoNome,
                CamposModificados = dicionarioCampos,
                Mensagem = message
            };

            string jsonString = JsonSerializer.Serialize(dadosLog);

            return new AuditoriaTicket
            {
                TicketId = ticketId,
                Log = jsonString,
                RegistradoEm = DateTime.UtcNow,
                Ativo = true
            };
        }
    }
}