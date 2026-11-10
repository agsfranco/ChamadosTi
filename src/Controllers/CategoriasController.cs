using ChamadosTi.Models;
using ChamadosTi.Services.Interfaces;
using ChamadosTi.ViewModels.Categoria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoriasController : Controller
    {
        private readonly ICategoriaService _categoriaService;
        private readonly IEspecialidadeService _especialidadeService;
        private readonly IStringLocalizer<CategoriasController> _localizer;

        public CategoriasController(ICategoriaService categoriaService, IEspecialidadeService especialidadeService, IStringLocalizer<CategoriasController> localizer)
        {
            _categoriaService = categoriaService;
            _especialidadeService = especialidadeService;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(bool exibirInativos = false)
        {
            var categorias = await _categoriaService.ListarAsync(exibirInativos);

            ViewBag.ExibirInativos = exibirInativos;
            return View(categorias);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Especialidades = await _especialidadeService.ListarOrdenadosPorNomeAsync();
            return View(new CategoriaViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoriaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Especialidades = await _especialidadeService.ListarOrdenadosPorNomeAsync();
                return View(model);
            }

            var novaCategoria = new Categoria
            {
                EspecialidadeId = model.EspecialidadeId,
                Nome = model.Nome,
                Descricao = model.Descricao,
                Ativo = true
            };

            await _categoriaService.CriarAsync(novaCategoria);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var categoria = await _categoriaService.ObterPorIdAsync(id);
            if (categoria == null) return NotFound();

            ViewBag.Especialidades = await _especialidadeService.ListarOrdenadosPorNomeAsync();

            var model = new CategoriaViewModel
            {
                Id = categoria.Id,
                EspecialidadeId = categoria.EspecialidadeId,
                Nome = categoria.Nome,
                Descricao = categoria.Descricao
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(CategoriaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Especialidades = await _especialidadeService.ListarOrdenadosPorNomeAsync();
                return View(model);
            }

            var categoria = await _categoriaService.ObterPorIdAsync(model.Id);
            if (categoria == null) return NotFound();

            categoria.EspecialidadeId = model.EspecialidadeId;
            categoria.Nome = model.Nome;
            categoria.Descricao = model.Descricao;

            await _categoriaService.AtualizarAsync(categoria);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var categoria = await _categoriaService.ObterPorIdAsync(id, incluirRelacionamentos: true);
            if (categoria == null) return NotFound();

            return View(categoria);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var categoria = await _categoriaService.ObterPorIdAsync(id);
            if (categoria == null) return NotFound();

            await _categoriaService.RemoverAsync(categoria);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var categoria = await _categoriaService.ObterPorIdAsync(id, ignorarFiltros: true);
            if (categoria == null) return NotFound();

            await _categoriaService.AtivarAsync(categoria);
            return RedirectToAction(nameof(Index));
        }
    }
}
