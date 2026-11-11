using ChamadosTi.Models;
using ChamadosTi.Services.Interfaces;
using ChamadosTi.ViewModels.Item;
using ChamadosTi.ViewModels.Ticket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ItensController : Controller
    {
        private readonly IItemService _itemService;
        private readonly ITipoService _tipoService;
        private readonly IMarcaService _marcaService;
        private readonly ISetorService _setorService;
        private readonly ITicketService _ticketService;
        private readonly IStringLocalizer<ItensController> _localizer;

        public ItensController(IItemService itemService, ITipoService tipoService, IMarcaService marcaService, ISetorService setorService, ITicketService ticketService, IStringLocalizer<ItensController> localizer)
        {
            _itemService = itemService;
            _tipoService = tipoService;
            _marcaService = marcaService;
            _setorService = setorService;
            _ticketService = ticketService;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(bool exibirInativos = false)
        {
            var itens = await _itemService.ListarAsync(exibirInativos);

            ViewBag.ExibirInativos = exibirInativos;
            return View(itens);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var item = await _itemService.ObterPorIdAsync(id, incluirRelacionamentos: true);
            if (item == null) return NotFound();

            var chamadosRelacionados = await _ticketService.ObterChamadosPorItemAsync(id);

            var historicoDTO = new List<TicketHistoricoAtivoDTO>();

            foreach (var ticket in chamadosRelacionados)
            {
                string parecer = string.Empty;
                if (ticket.AuditoriaTickets != null && ticket.AuditoriaTickets.Any())
                {
                    var ultimaAuditoria = ticket.AuditoriaTickets
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
                    Id = ticket.Id,
                    Titulo = ticket.Titulo,
                    DataAbertura = ticket.DataAbertura.ToLocalTime().ToString("d"),
                    StatusNome = ticket.Status.Nome,
                    ParecerTecnico = !string.IsNullOrEmpty(parecer) ? parecer : null
                });
            }

            ViewBag.ChamadosRelacionados = historicoDTO;

            return View(item);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new ItemViewModel();
            await CarregaDropdownsAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CarregaDropdownsAsync(model);
                return View(model);
            }

            var novoItem = new Item
            {
                TipoId = model.TipoId!.Value,
                MarcaId = model.MarcaId!.Value,
                SetorId = model.SetorId,
                Nome = model.Nome,
                Comentario = model.Comentario,
                CriadoEm = DateTime.UtcNow,
                Ativo = true
            };

            await _itemService.CriarAsync(novoItem);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var item = await _itemService.ObterPorIdAsync(id);
            if (item == null) return NotFound();

            var model = new ItemViewModel
            {
                Tipos = await _tipoService.ListarOrdenadosPorNomeAsync(),
                Marcas = await _marcaService.ListarOrdenadosPorNomeAsync(),
                Setores = await _setorService.ListarOrdenadosPorNomeAsync(),
                Id = item.Id,
                TipoId = item.TipoId,
                MarcaId = item.MarcaId,
                SetorId = item.SetorId,
                Nome = item.Nome,
                Comentario = item.Comentario
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CarregaDropdownsAsync(model);
                return View(model);
            }

            var item = await _itemService.ObterPorIdAsync(model.Id);
            if (item == null) return NotFound();

            item.TipoId = model.TipoId!.Value;
            item.MarcaId = model.MarcaId!.Value;
            item.SetorId = model.SetorId;
            item.Nome = model.Nome;
            item.Comentario = model.Comentario;

            await _itemService.AtualizarAsync(item);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _itemService.ObterPorIdAsync(id, incluirRelacionamentos: true);
            if (item == null) return NotFound();

            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _itemService.ObterPorIdAsync(id);
            if (item == null) return NotFound();

            await _itemService.RemoverAsync(item);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var item = await _itemService.ObterPorIdAsync(id, ignorarFiltros: true);
            if (item == null) return NotFound();

            await _itemService.AtivarAsync(item);
            return RedirectToAction(nameof(Index));
        }

        private async Task CarregaDropdownsAsync(ItemViewModel model)
        {
            model.Tipos = await _tipoService.ListarOrdenadosPorNomeAsync();
            model.Marcas = await _marcaService.ListarOrdenadosPorNomeAsync();
            model.Setores = await _setorService.ListarOrdenadosPorNomeAsync();
        }
    }
}
