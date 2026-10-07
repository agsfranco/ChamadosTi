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

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Categorias = _context.Categorias.OrderBy(c => c.Nome).ToList();
            ViewBag.Itens = _context.Itens.Include(i => i.Tipos).OrderBy(i => i.Nome).ToList();
            return View(new TicketCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TicketCreateViewModel model)
        {
            var identityUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(identityUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            var usuarioSistema = _context.Usuarios.FirstOrDefault(u => u.AspNetUserId == identityUserId);
            if (usuarioSistema == null)
            {
                return RedirectToAction("CompleteProfile", "Account");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = _context.Categorias.OrderBy(c => c.Nome).ToList();
                ViewBag.Itens = _context.Itens.Include(i => i.Tipos).OrderBy(i => i.Nome).ToList();
                return View(model);
            }

            var prioridadePadrao = _context.Prioridades.FirstOrDefault(p => p.Nome.Contains("Média") || p.Nome.Contains("Medium")) 
                                   ?? _context.Prioridades.FirstOrDefault();

            if (prioridadePadrao == null)
            {
                // Tradução injetada dinamicamente via recurso do Controlador
                ModelState.AddModelError("", _localizer["Error_SemPrioridade"]);
                ViewBag.Categorias = _context.Categorias.OrderBy(c => c.Nome).ToList();
                ViewBag.Itens = _context.Itens.Include(i => i.Tipos).OrderBy(i => i.Nome).ToList();
                return View(model);
            }

            var statusInicial = _context.Status.FirstOrDefault(s => s.Codigo == (int)TicketStatusEnum.Aberto);
            if (statusInicial == null)
            {
                // Tradução injetada dinamicamente via recurso do Controlador
                ModelState.AddModelError("", _localizer["Error_SemStatusInicial"]);
                ViewBag.Categorias = _context.Categorias.OrderBy(c => c.Nome).ToList();
                ViewBag.Itens = _context.Itens.Include(i => i.Tipos).OrderBy(i => i.Nome).ToList();
                return View(model);
            }

            var dataAberturaUtc = DateTime.UtcNow;

            var prazoResposta = SlaCalculator.CalcularPrazoSlaReal(_context, usuarioSistema.SetorId, dataAberturaUtc, prioridadePadrao.TempoRespostaPadrao);
            var prazoResolucao = SlaCalculator.CalcularPrazoSlaReal(_context, usuarioSistema.SetorId, dataAberturaUtc, prioridadePadrao.TempoResolucaoPadrao);

            var novoTicket = new Ticket
            {
                UsuarioId = usuarioSistema.Id,
                SetorId = usuarioSistema.SetorId,
                CategoriaId = model.CategoriaId!.Value,
                PrioridadeId = prioridadePadrao.Id,
                StatusId = statusInicial.Id,
                DataAbertura = dataAberturaUtc,
                PrazoRespostaSLA = prazoResposta,
                PrazoResolucaoSLA = prazoResolucao,
                Titulo = model.Titulo,
                Comentario = model.Comentario,
                Ativo = true
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

            // PADRONIZAÇÃO DE AUDITORIA INTERNACIONAL: O JSON agora salva CHAVES lógicas em vez de strings fixas em português
            var dadosLog = new LogAuditoriaTicket
            {
                Acao = "Log_AberturaChamado",
                Usuario = usuarioSistema.Nome,
                CamposModificados = new Dictionary<string, string>
                {
                    { "Log_StatusInicial", "Status_Aberto" },
                    { "Log_PrazoResposta", prazoResposta.ToLocalTime().ToString("g") },
                    { "Log_PrazoResolucao", prazoResolucao.ToLocalTime().ToString("g") }
                }
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
            return RedirectToAction("Index", "Home");
        }
    }
}
