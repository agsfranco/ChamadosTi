using ChamadosTi.Models;
using ChamadosTi.Services.Interfaces;
using ChamadosTi.ViewModels.Feriado;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FeriadosController : Controller
    {
        private readonly IFeriadoService _feriadoService;
        private readonly IStringLocalizer<FeriadosController> _localizer;

        public FeriadosController(IFeriadoService feriadoService, IStringLocalizer<FeriadosController> localizer)
        {
            _feriadoService = feriadoService;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(bool exibirInativos = false)
        {
            var feriados = await _feriadoService.ListarAsync(exibirInativos);

            ViewBag.ExibirInativos = exibirInativos;
            return View(feriados);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new FeriadoViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FeriadoViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _feriadoService.DiaDuplicadoAsync(model.Dia!.Value, model.Id))
            {
                ModelState.AddModelError("Dia", _localizer["Error_FeriadoDuplicado"]);
                return View(model);
            }

            var novoFeriado = new Feriado
            {
                Nome = model.Nome,
                Dia = model.Dia!.Value,
                Ativo = true
            };

            await _feriadoService.CriarAsync(novoFeriado);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var feriado = await _feriadoService.ObterPorIdAsync(id);
            if (feriado == null) return NotFound();

            var model = new FeriadoViewModel
            {
                Id = feriado.Id,
                Nome = feriado.Nome,
                Dia = feriado.Dia
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(FeriadoViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var feriado = await _feriadoService.ObterPorIdAsync(model.Id);
            if (feriado == null) return NotFound();

            if (await _feriadoService.DiaDuplicadoAsync(model.Dia!.Value, model.Id))
            {
                ModelState.AddModelError("Dia", _localizer["Error_FeriadoDuplicado"]);
                return View(model);
            }

            feriado.Nome = model.Nome;
            feriado.Dia = model.Dia!.Value;

            await _feriadoService.AtualizarAsync(feriado);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var feriado = await _feriadoService.ObterPorIdAsync(id);
            if (feriado == null) return NotFound();

            return View(feriado);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var feriado = await _feriadoService.ObterPorIdAsync(id);
            if (feriado == null) return NotFound();

            await _feriadoService.RemoverAsync(feriado);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var feriado = await _feriadoService.ObterPorIdAsync(id, ignorarFiltros: true);
            if (feriado == null) return NotFound();

            await _feriadoService.AtivarAsync(feriado);
            return RedirectToAction(nameof(Index));
        }
    }
}
