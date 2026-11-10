using ChamadosTi.Models;
using ChamadosTi.Services.Interfaces;
using ChamadosTi.ViewModels.Setor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SetoresController : Controller
    {
        private readonly ISetorService _setorService;
        private readonly IStringLocalizer<SetoresController> _localizer;

        public SetoresController(ISetorService setorService, IStringLocalizer<SetoresController> localizer)
        {
            _setorService = setorService;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(bool exibirInativos = false)
        {
            var setores = await _setorService.ListarAsync(exibirInativos);

            ViewBag.ExibirInativos = exibirInativos;
            return View(setores);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new SetorViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Setor model)
        {
            if (!ModelState.IsValid) return View(model);

            var novoSetor = new Setor
            {
                Nome = model.Nome,
                Descricao = model.Descricao,
                Ti = model.Ti,
                Ativo = true
            };

            await _setorService.CriarAsync(novoSetor);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var setor = await _setorService.ObterPorIdAsync(id);
            if (setor == null) return NotFound();

            var model = new SetorViewModel
            {
                Id = setor.Id,
                Nome = setor.Nome,
                Descricao = setor.Descricao,
                Ti = setor.Ti
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(SetorViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var setor = await _setorService.ObterPorIdAsync(model.Id);
            if (setor == null) return NotFound();

            if (!_setorService.PodeAlterarFlagTi(setor, model.Ti))
            {
                ModelState.AddModelError("Ti", _localizer["Error_SetorTiNaoPodeDeixarDeSerTi"]);
                return View(model);
            }

            setor.Nome = model.Nome;
            setor.Descricao = model.Descricao;
            setor.Ti = model.Ti;

            await _setorService.AtualizarAsync(setor);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var setor = await _setorService.ObterPorIdAsync(id);
            if (setor == null) return NotFound();

            if (!_setorService.PodeSerExcluido(setor))
            {
                TempData["ErrorMessage"] = _localizer["Error_SetorTiNaoPodeSerExcluido"].ToString();
                return RedirectToAction(nameof(Index));
            }

            await _setorService.RemoverAsync(setor);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var setor = await _setorService.ObterPorIdAsync(id);
            if (setor == null) return NotFound();

            if (!_setorService.PodeSerExcluido(setor))
            {
                TempData["ErrorMessage"] = _localizer["Error_SetorTiExc"].ToString();
                return RedirectToAction(nameof(Index));
            }

            await _setorService.RemoverAsync(setor);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var setor = await _setorService.ObterPorIdAsync(id, ignorarFiltros: true);
            if (setor == null) return NotFound();

            await _setorService.AtivarAsync(setor);
            return RedirectToAction(nameof(Index));
        }
    }
}
