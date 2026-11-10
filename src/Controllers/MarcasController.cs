using ChamadosTi.Models;
using ChamadosTi.Services.Interfaces;
using ChamadosTi.ViewModels.Marca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MarcasController : Controller
    {
        private readonly IMarcaService _marcaService;
        private readonly IStringLocalizer<MarcasController> _localizer;

        public MarcasController(IMarcaService marcaService, IStringLocalizer<MarcasController> localizer)
        {
            _marcaService = marcaService;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(bool exibirInativos = false)
        {
            var marcas = await _marcaService.ListarAsync(exibirInativos);

            ViewBag.ExibirInativos = exibirInativos;
            return View(marcas);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new MarcaViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MarcaViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var novaMarca = new Marca
            {
                Nome = model.Nome,
                Ativo = true
            };

            await _marcaService.CriarAsync(novaMarca);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var marca = await _marcaService.ObterPorIdAsync(id);
            if (marca == null) return NotFound();

            var model = new MarcaViewModel
            {
                Id = marca.Id,
                Nome = marca.Nome
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(MarcaViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var marca = await _marcaService.ObterPorIdAsync(model.Id);
            if (marca == null) return NotFound();

            marca.Nome = model.Nome;

            await _marcaService.AtualizarAsync(marca);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var marca = await _marcaService.ObterPorIdAsync(id);
            if (marca == null) return NotFound();

            return View(marca);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var marca = await _marcaService.ObterPorIdAsync(id);
            if (marca == null) return NotFound();

            await _marcaService.RemoverAsync(marca);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var marca = await _marcaService.ObterPorIdAsync(id, ignorarFiltros: true);
            if (marca == null) return NotFound();

            await _marcaService.AtivarAsync(marca);
            return RedirectToAction(nameof(Index));
        }
    }
}
