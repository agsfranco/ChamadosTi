using ChamadosTi.Models;
using ChamadosTi.Services.Interfaces;
using ChamadosTi.ViewModels.Prioridade;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PrioridadesController : Controller
    {
        private readonly IPrioridadeService _prioridadeService;
        private readonly IStringLocalizer<PrioridadesController> _localizer;

        public PrioridadesController(IPrioridadeService prioridadeService, IStringLocalizer<PrioridadesController> localizer)
        {
            _prioridadeService = prioridadeService;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(bool exibirInativos = false)
        {
            var prioridades = await _prioridadeService.ListarAsync(exibirInativos);

            ViewBag.ExibirInativos = exibirInativos;
            return View(prioridades);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new PrioridadeViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PrioridadeViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _prioridadeService.PadraoDuplicadoAsync(model.Id, model.Padrao))
            {
                ModelState.AddModelError("Padrao", _localizer["Error_PadraoJaExistente"]);
                return View(model);
            }

            var novaPrioridade = new Prioridade
            {
                Nome = model.Nome,
                TempoRespostaPadrao = model.TempoRespostaPadrao,
                TempoResolucaoPadrao = model.TempoResolucaoPadrao,
                Padrao = model.Padrao,
                Ativo = true
            };

            await _prioridadeService.CriarAsync(novaPrioridade);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var prioridade = await _prioridadeService.ObterPorIdAsync(id);
            if (prioridade == null) return NotFound();

            var model = new PrioridadeViewModel
            {
                Id = prioridade.Id,
                Nome = prioridade.Nome,
                TempoRespostaPadrao = prioridade.TempoRespostaPadrao,
                TempoResolucaoPadrao = prioridade.TempoResolucaoPadrao,
                Padrao = prioridade.Padrao
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(PrioridadeViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _prioridadeService.PadraoDuplicadoAsync(model.Id, model.Padrao))
            {
                ModelState.AddModelError("Padrao", _localizer["Error_PadraoJaExistente"]);
                return View(model);
            }

            var prioridade = await _prioridadeService.ObterPorIdAsync(model.Id);
            if (prioridade == null) return NotFound();

            prioridade.Nome = model.Nome;
            prioridade.TempoRespostaPadrao = model.TempoRespostaPadrao;
            prioridade.TempoResolucaoPadrao = model.TempoResolucaoPadrao;
            prioridade.Padrao = model.Padrao;

            await _prioridadeService.AtualizarAsync(prioridade);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var prioridade = await _prioridadeService.ObterPorIdAsync(id);
            if (prioridade == null) return NotFound();

            return View(prioridade);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var prioridade = await _prioridadeService.ObterPorIdAsync(id);
            if (prioridade == null) return NotFound();

            await _prioridadeService.RemoverAsync(prioridade);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var prioridade = await _prioridadeService.ObterPorIdAsync(id, ignorarFiltros: true);
            if (prioridade == null) return NotFound();

            await _prioridadeService.AtivarAsync(prioridade);
            return RedirectToAction(nameof(Index));
        }
    }
}
