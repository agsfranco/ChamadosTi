using ChamadosTi.Models;
using ChamadosTi.Services.Interfaces;
using ChamadosTi.ViewModels.Tipo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TiposController : Controller
    {
        private readonly ITipoService _tipoService;
        private readonly IStringLocalizer<TiposController> _localizer;

        public TiposController(ITipoService tipoService, IStringLocalizer<TiposController> localizer)
        {
            _tipoService = tipoService;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(bool exibirInativos = false)
        {
            var tipos = await _tipoService.ListarAsync(exibirInativos);

            ViewBag.ExibirInativos = exibirInativos;
            return View(tipos);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new TipoViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TipoViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var novoTipo = new Tipo
            {
                Nome = model.Nome,
                Comentario = model.Comentario,
                Ativo = true
            };

            await _tipoService.CriarAsync(novoTipo);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var tipo = await _tipoService.ObterPorIdAsync(id);
            if (tipo == null) return NotFound();

            var model = new TipoViewModel
            {
                Id = tipo.Id,
                Nome = tipo.Nome,
                Comentario = tipo.Comentario
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(TipoViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var tipo = await _tipoService.ObterPorIdAsync(model.Id);
            if (tipo == null) return NotFound();

            tipo.Nome = model.Nome;
            tipo.Comentario = model.Comentario;

            await _tipoService.AtualizarAsync(tipo);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var tipo = await _tipoService.ObterPorIdAsync(id);
            if (tipo == null) return NotFound();

            return View(tipo);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tipo = await _tipoService.ObterPorIdAsync(id);
            if (tipo == null) return NotFound();

            await _tipoService.RemoverAsync(tipo);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var tipo = await _tipoService.ObterPorIdAsync(id, ignorarFiltros: true);
            if (tipo == null) return NotFound();

            await _tipoService.AtivarAsync(tipo);
            return RedirectToAction(nameof(Index));
        }
    }
}
