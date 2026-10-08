using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.Utils;
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

        public TicketsController(AppDbContext context, UserManager<IdentityUser> userManager, IStringLocalizer<TicketsController> localizer)
        {
            _context = context;
            _userManager = userManager;
            _localizer = localizer;
        }

        // GET: Tickets
        public IActionResult Index()
        {
            var tickets = _context.Tickets
                .Include(t => t.Status)
                .Include(t => t.Prioridades)
                .Include(t => t.Categorias)
                .Include(t => t.Usuarios) 
                .Include(t => t.UsuariosTecnicos) 
                .OrderByDescending(t => t.DataAbertura) 
                .ToList();

            return View(tickets);
        }

        [HttpGet]
        public IActionResult Create(int? origemId = null)
        {
            var usuarioSistema = GetUsuario();
            if (usuarioSistema is null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Categorias = _context.Categorias.OrderBy(c => c.Nome).ToList();
            MontaListaItens(usuarioSistema.SetorId);

            ViewBag.TicketOrigemId = origemId;

            return View(new TicketCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TicketCreateViewModel model, int? ticketOrigemId)
        {
            var usuarioSistema = GetUsuario();
            if (usuarioSistema is null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = _context.Categorias.OrderBy(c => c.Nome).ToList();
                MontaListaItens(usuarioSistema.SetorId);
                ViewBag.TicketOrigemId = ticketOrigemId;
                return View(model);
            }

            var prioridadePadrao = _context.Prioridades.FirstOrDefault(p => p.Padrao == true);
            if (prioridadePadrao == null)
            {
                ModelState.AddModelError("", _localizer["Error_SemPrioridade"]);
                ViewBag.Categorias = _context.Categorias.OrderBy(c => c.Nome).ToList();
                MontaListaItens(usuarioSistema.SetorId);
                return View(model);
            }

            var statusInicial = _context.Status.FirstOrDefault(s => s.Codigo == (int)TicketStatusEnum.Aberto);
            if (statusInicial == null)
            {
                ModelState.AddModelError("", _localizer["Error_SemStatusInicial"]);
                ViewBag.Categorias = _context.Categorias.OrderBy(c => c.Nome).ToList();
                MontaListaItens(usuarioSistema.SetorId);
                return View(model);
            }

            var dataAberturaUtc = DateTime.UtcNow;

            var dataHoje = DateOnly.FromDateTime(dataAberturaUtc);
            var todosFeriados = _context.Feriados
                .Where(f => f.Dia >= dataHoje)
                .Select(f => f.Dia)
                .ToList();
            
            var setorTiDoSistema = _context.Setores.FirstOrDefault(s => s.Ti == true);
            int setorSlaId = setorTiDoSistema != null ? setorTiDoSistema.Id : usuarioSistema.SetorId;

            var horariosSetores = _context.SetoresHorarios
                .Include(s => s.Horario)
                .Where(s => s.SetorId == setorSlaId)
                .Select(s => s.Horario)
                .ToList();

            var prazoRespostaUtc = SlaCalculator.CalcularPrazoSlaReal(todosFeriados, horariosSetores, dataAberturaUtc, prioridadePadrao.TempoRespostaPadrao);
            var prazoResolucaoUtc = SlaCalculator.CalcularPrazoSlaReal(todosFeriados, horariosSetores, dataAberturaUtc, prioridadePadrao.TempoResolucaoPadrao);

            var novoTicket = new Ticket
            {
                UsuarioId = usuarioSistema.Id,
                SetorId = usuarioSistema.SetorId,
                CategoriaId = model.CategoriaId!.Value,
                PrioridadeId = prioridadePadrao.Id,
                StatusId = statusInicial.Id,
                DataAbertura = dataAberturaUtc,
                PrazoRespostaSLA = prazoRespostaUtc,
                PrazoResolucaoSLA = prazoResolucaoUtc,
                Titulo = model.Titulo,
                Comentario = model.Comentario,
                Ativo = true,
                TicketOrigemId = ticketOrigemId 
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
                { "Log_StatusInicial", "Status_Aberto" },
                { "Log_PrazoResposta", prazoRespostaUtc.ToString("g") },
                { "Log_PrazoResolucao", prazoResolucaoUtc.ToString("g") }
            };

            if (ticketOrigemId != null)
            {
                dicionarioCampos.Add("Log_DerivadoDoTicket", ticketOrigemId.Value.ToString());
            }

            var dadosLog = new LogAuditoriaTicket
            {
                Acao = "Log_AberturaChamado",
                Usuario = usuarioSistema.Nome,
                CamposModificados = dicionarioCampos
            };

            string jsonString = JsonSerializer.Serialize(dadosLog);

            _context.AuditoriasTickets.Add(new AuditoriaTicket
            {
                TicketId = novoTicket.Id,
                Log = jsonString,
                RegistradoEm = dataAberturaUtc,
                Ativo = true
            });

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Tickets");
        }
        
        [HttpGet]
        public IActionResult Details(int id)
        {
            var usuarioSistema = GetUsuario();
            if (usuarioSistema is null)
            {
                return RedirectToAction("Login", "Account");
            }

            var ticket = _context.Tickets
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
                .FirstOrDefault(t => t.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            var auditorias = _context.AuditoriasTickets
                .Where(a => a.TicketId == id)
                .OrderBy(a => a.RegistradoEm)
                .ToList();

            var listaLogsDecodificados = new List<LogAuditoriaTicket>();

            foreach (var auditoria in auditorias)
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
                        Acao = "Error_LogCorrompido", 
                        Usuario = "System" 
                    });
                }
            }

            ViewBag.LogsHistorico = listaLogsDecodificados;
            ViewBag.AuditoriasOriginais = auditorias; 

            return View(ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assumir(int id)
        {
            var tecnicoSistema = GetUsuario();
            if (tecnicoSistema is null)
            {
                return RedirectToAction("Login", "Account");
            }

            var ticket = _context.Tickets
                .Include(t => t.Status)
                .FirstOrDefault(t => t.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            if (ticket.Status.Codigo != (int)TicketStatusEnum.Aberto)
            {
                TempData["ErrorMessage"] = _localizer["Error_ChamadoJaAssumidoOuFechado"].Value;
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            var novoStatus = _context.Status.FirstOrDefault(s => s.Codigo == (int)TicketStatusEnum.EmAtendimento);
            if (novoStatus == null)
            {
                TempData["ErrorMessage"] = _localizer["Error_StatusAtendimentoNaoEncontrado"].Value;
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            ticket.UsuarioTecnicoId = tecnicoSistema.Id;
            ticket.StatusId = novoStatus.Id;

            _context.Tickets.Update(ticket);

            var dataInteracaoUtc = DateTime.UtcNow;
            var dadosLog = new LogAuditoriaTicket
            {
                Acao = "Log_ChamadoAssumido",
                Usuario = tecnicoSistema.Nome,
                CamposModificados = new Dictionary<string, string>
                {
                    { "Log_StatusAlterado", "Status_EmAtendimento" },
                    { "Log_TecnicoAtribuido", tecnicoSistema.Nome }
                }
            };

            string jsonString = JsonSerializer.Serialize(dadosLog);

            _context.AuditoriasTickets.Add(new AuditoriaTicket
            {
                TicketId = ticket.Id,
                Log = jsonString,
                RegistradoEm = dataInteracaoUtc,
                Ativo = true
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Success_ChamadoAssumido"].Value;
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarComentario(int ticketId, string mensagemTexto)
        {
            var usuarioLogado = GetUsuario();
            if (usuarioLogado is null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(mensagemTexto))
            {
                TempData["ErrorMessage"] = _localizer["Error_ComentarioVazio"].Value;
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            var ticketExiste = _context.Tickets.Any(t => t.Id == ticketId);
            if (!ticketExiste)
            {
                return NotFound();
            }

            var dataRegistroUtc = DateTime.UtcNow;

            var dadosLog = new LogAuditoriaTicket
            {
                Acao = "Log_NovaInteracao",
                Usuario = usuarioLogado.Nome,
                Mensagem = mensagemTexto.Trim(),
                CamposModificados = null
            };

            string jsonString = JsonSerializer.Serialize(dadosLog);

            var novoComentarioAuditoria = new AuditoriaTicket
            {
                TicketId = ticketId,
                Log = jsonString,
                RegistradoEm = dataRegistroUtc,
                Ativo = true
            };

            _context.AuditoriasTickets.Add(novoComentarioAuditoria);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Success_ComentarioAdicionado"].Value;
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoverParaPendente(int id)
        {
            var tecnicoSistema = GetUsuario();
            if (tecnicoSistema is null) return RedirectToAction("Login", "Account");

            var ticket = _context.Tickets.Include(t => t.Status).FirstOrDefault(t => t.Id == id);
            if (ticket == null) return NotFound();

            if (ticket.Status.Codigo != (int)TicketStatusEnum.EmAtendimento)
            {
                TempData["ErrorMessage"] = _localizer["Error_StatusInvalidoParaTramitar"].Value;
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            var statusPendente = _context.Status.FirstOrDefault(s => s.Codigo == (int)TicketStatusEnum.Pendente);
            if (statusPendente == null) return NotFound();

            ticket.StatusId = statusPendente.Id;
            _context.Tickets.Update(ticket);

            var dadosLog = new LogAuditoriaTicket
            {
                Acao = "Log_ChamadoPendente",
                Usuario = tecnicoSistema.Nome,
                CamposModificados = new Dictionary<string, string> { { "Log_StatusAlterado", "Status_Pendente" } }
            };

            _context.AuditoriasTickets.Add(new AuditoriaTicket
            {
                TicketId = ticket.Id,
                Log = JsonSerializer.Serialize(dadosLog),
                RegistradoEm = DateTime.UtcNow,
                Ativo = true
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = _localizer["Success_ChamadoPendente"].Value;
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RetomarAtendimento(int id)
        {
            var tecnicoSistema = GetUsuario();
            if (tecnicoSistema is null) return RedirectToAction("Login", "Account");

            var ticket = _context.Tickets.Include(t => t.Status).FirstOrDefault(t => t.Id == id);
            if (ticket == null) return NotFound();

            if (ticket.Status.Codigo != (int)TicketStatusEnum.Pendente)
            {
                TempData["ErrorMessage"] = _localizer["Error_StatusInvalidoParaTramitar"].Value;
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            var statusAtendimento = _context.Status.FirstOrDefault(s => s.Codigo == (int)TicketStatusEnum.EmAtendimento);
            if (statusAtendimento == null) return NotFound();

            ticket.StatusId = statusAtendimento.Id;
            _context.Tickets.Update(ticket);

            var dadosLog = new LogAuditoriaTicket
            {
                Acao = "Log_ChamadoRetomado",
                Usuario = tecnicoSistema.Nome,
                CamposModificados = new Dictionary<string, string> { { "Log_StatusAlterado", "Status_EmAtendimento" } }
            };

            _context.AuditoriasTickets.Add(new AuditoriaTicket
            {
                TicketId = ticket.Id,
                Log = JsonSerializer.Serialize(dadosLog),
                RegistradoEm = DateTime.UtcNow,
                Ativo = true
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = _localizer["Success_ChamadoRetomado"].Value;
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoverParaResolvido(int id, string solucaoTexto)
        {
            var tecnicoSistema = GetUsuario();
            if (tecnicoSistema is null) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(solucaoTexto))
            {
                TempData["ErrorMessage"] = _localizer["Error_ParecerObrigatorio"].Value;
                return RedirectToAction(nameof(Details), new { id = id });
            }

            var ticket = _context.Tickets.Include(t => t.Status).FirstOrDefault(t => t.Id == id);
            if (ticket == null) return NotFound();

            if (ticket.Status.Codigo != (int)TicketStatusEnum.EmAtendimento && ticket.Status.Codigo != (int)TicketStatusEnum.Pendente)
            {
                TempData["ErrorMessage"] = _localizer["Error_StatusInvalidoParaTramitar"].Value;
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            var statusResolvido = _context.Status.FirstOrDefault(s => s.Codigo == (int)TicketStatusEnum.Resolvido);
            if (statusResolvido == null) return NotFound();

            ticket.StatusId = statusResolvido.Id;
            ticket.DataFechamento = DateTime.UtcNow; 
            _context.Tickets.Update(ticket);

            var dadosLog = new LogAuditoriaTicket
            {
                Acao = "Log_ChamadoResolvido",
                Usuario = tecnicoSistema.Nome,
                Mensagem = solucaoTexto.Trim(),
                CamposModificados = new Dictionary<string, string> { { "Log_StatusAlterado", "Status_Resolvido" } }
            };

            _context.AuditoriasTickets.Add(new AuditoriaTicket
            {
                TicketId = ticket.Id,
                Log = JsonSerializer.Serialize(dadosLog),
                RegistradoEm = DateTime.UtcNow,
                Ativo = true
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = _localizer["Success_ChamadoResolvido"].Value;
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id, string justificativaTexto)
        {
            var usuarioLogado = GetUsuario();
            if (usuarioLogado is null) return RedirectToAction("Login", "Account");

            // Validação de segurança: Impede o envio de justificativa vazia
            if (string.IsNullOrWhiteSpace(justificativaTexto))
            {
                TempData["ErrorMessage"] = _localizer["Error_JustificativaObrigatoria"].Value;
                return RedirectToAction(nameof(Details), new { id = id });
            }

            var ticket = _context.Tickets.Include(t => t.Status).FirstOrDefault(t => t.Id == id);
            if (ticket == null) return NotFound();

            if (ticket.Status.Codigo >= (int)TicketStatusEnum.Resolvido)
            {
                TempData["ErrorMessage"] = _localizer["Error_StatusInvalidoParaCancelar"].Value;
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            var statusCancelado = _context.Status.FirstOrDefault(s => s.Codigo == (int)TicketStatusEnum.Cancelado);
            if (statusCancelado == null) return NotFound();

            ticket.StatusId = statusCancelado.Id;
            ticket.DataFechamento = DateTime.UtcNow; 
            _context.Tickets.Update(ticket);

            // Gravação Sênior: A justificativa é acoplada na propriedade 'Mensagem' do JSON de Auditoria
            var dadosLog = new LogAuditoriaTicket
            {
                Acao = "Log_ChamadoCancelado",
                Usuario = usuarioLogado.Nome,
                Mensagem = justificativaTexto.Trim(), // Salva a justificativa na linha do tempo
                CamposModificados = new Dictionary<string, string> { { "Log_StatusAlterado", "Status_Cancelado" } }
            };

            _context.AuditoriasTickets.Add(new AuditoriaTicket
            {
                TicketId = ticket.Id,
                Log = JsonSerializer.Serialize(dadosLog),
                RegistradoEm = DateTime.UtcNow,
                Ativo = true
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = _localizer["Success_ChamadoCancelado"].Value;
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        private Usuario? GetUsuario()
        {
            var identityUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(identityUserId))
            {
                return null;
            }

            return _context.Usuarios.FirstOrDefault(u => u.AspNetUserId == identityUserId);
        }

        private void MontaListaItens(int setorId)
        {
            ViewBag.Itens =  _context.Itens
                             .Include(i => i.Tipos)
                             .Include(i => i.Marcas)
                             .Where(i => i.SetorId == setorId)
                             .OrderBy(i => i.Nome)
                             .ThenBy(i => i.Tipos.Nome)
                             .ThenBy(i => i.Marcas.Nome)
                             .Select(i => new { Id = i.Id, TextOpcional = i.Nome + " - " + i.Tipos.Nome + " - " + i.Marcas.Nome }).ToList();
        }
    }
}
