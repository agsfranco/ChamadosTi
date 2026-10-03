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

        // GET: Tickets/Create
        [HttpGet]
        public IActionResult Create()
        {
            // Carrega apenas as categorias ativas em ordem alfabética
            ViewBag.Categorias = _context.Categorias.OrderBy(c => c.Nome).ToList();

            // Carrega os itens/ativos de hardware ativos para o usuário vincular se necessário
            ViewBag.Itens = _context.Itens.Include(i => i.Tipos).OrderBy(i => i.Nome).ToList();

            return View(new TicketCreateViewModel());
        }

        // POST: Tickets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TicketCreateViewModel model)
        {
            var identityUserId = _userManager.GetUserId(User);
            
            // Segurança contra nulos para o compilador
            if (string.IsNullOrEmpty(identityUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Busca o Usuário numérico correspondente na nossa tabela personalizada
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

            // Definição de Regra de Negócio: Buscaremos a prioridade "Média" como padrão inicial.
            // Caso não exista no banco, pegamos a primeira ativa disponível.
            var prioridadePadrao = _context.Prioridades.FirstOrDefault(p => p.Nome.Contains("Média") || p.Nome.Contains("Medium")) 
                                   ?? _context.Prioridades.FirstOrDefault();

            if (prioridadePadrao == null)
            {
                ModelState.AddModelError("", "Nenhuma prioridade (SLA) cadastrada no sistema. Contate o administrador.");
                ViewBag.Categorias = _context.Categorias.OrderBy(c => c.Nome).ToList();
                ViewBag.Itens = _context.Itens.Include(i => i.Tipos).OrderBy(i => i.Nome).ToList();
                return View(model);
            }

            // Busca o registro do Status "Aberto" (Código 1 do nosso Enum estático)
            var statusInicial = _context.Status.FirstOrDefault(s => s.Codigo == (int)TicketStatusEnum.Aberto);
            if (statusInicial == null)
            {
                ModelState.AddModelError("", "Status inicial 'Aberto' não localizado na carga do banco.");
                ViewBag.Categorias = _context.Categorias.OrderBy(c => c.Nome).ToList();
                ViewBag.Itens = _context.Itens.Include(i => i.Tipos).OrderBy(i => i.Nome).ToList();
                return View(model);
            }

            var dataAberturaUtc = DateTime.UtcNow;

            // MOTOR DE SLA: Calcula os prazos pulando finais de semana usando a classe utilitária
            var prazoResposta = SlaCalculator.CalcularPrazoDiasUteis(dataAberturaUtc, prioridadePadrao.TempoRespostaPadrao);
            var prazoResolucao = SlaCalculator.CalcularPrazoDiasUteis(dataAberturaUtc, prioridadePadrao.TempoResolucaoPadrao);

            // Monta a entidade principal do Ticket
            var novoTicket = new Ticket
            {
                UsuarioId = usuarioSistema.Id,
                SetorId = usuarioSistema.SetorId, // Vincula automaticamente o chamado ao setor atual do usuário
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
            await _context.SaveChangesAsync(); // Registra primeiro o Ticket para gerar o ID dele

            // 1. Gravação dos Itens/Ativos afetados na tabela de ligação
            if (model.ItensIdsSelecionados != null && model.ItensIdsSelecionados.Any())
            {
                foreach (var itemId in model.ItensIdsSelecionados)
                {
                    var itemTicket = new ItemTicket
                    {
                        TicketId = novoTicket.Id,
                        ItemId = itemId,
                        Ativo = true
                    };
                    _context.ItensTickets.Add(itemTicket);
                }
            }

            // 2. Criação do Objeto C# de Log e conversão em TEXTO JSON
            var dadosLog = new LogAuditoriaTicket
            {
                Acao = "Abertura do Chamado",
                Usuario = usuarioSistema.Nome,
                CamposModificados = new Dictionary<string, string>
                {
                    { "Status Inicial", "Aberto" },
                    { "SLA Resposta Calculado", prazoResposta.ToLocalTime().ToString("g") },
                    { "SLA Resolução Calculado", prazoResolucao.ToLocalTime().ToString("g") }
                }
            };

            string jsonString = JsonSerializer.Serialize(dadosLog);

            // Salva na tabela física de auditoria atrelada ao Ticket
            var auditoriaInicial = new AuditoriaTicket
            {
                TicketId = novoTicket.Id,
                Log = jsonString,
                RegistradoEm = dataAberturaUtc,
                Ativo = true
            };

            _context.AuditoriasTickets.Add(auditoriaInicial);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home"); // Temporário até criarmos a listagem de chamados
        }
    }
}
