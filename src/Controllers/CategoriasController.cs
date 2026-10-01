using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.ViewModels.Categoria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize]
    public class CategoriasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<CategoriasController> _localizer;

        public CategoriasController(AppDbContext context, IStringLocalizer<CategoriasController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public IActionResult Index(bool exibirInativos = false)
        {
            List<Categoria> categorias;

            if (exibirInativos)
            {
                categorias = _context.Categorias
                    .IgnoreQueryFilters()
                    .Include(c => c.Especialidades) // Carrega a propriedade de navegação
                    .Where(c => !c.Ativo)
                    .OrderBy(c => c.Nome)
                    .ToList();
            }
            else
            {
                categorias = _context.Categorias
                    .Include(c => c.Especialidades)
                    .OrderBy(c => c.Nome)
                    .ToList();
            }

            ViewBag.ExibirInativos = exibirInativos;
            return View(categorias);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Especialidades = _context.Especialidades.OrderBy(e => e.Nome).ToList();
            return View(new CategoriaViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoriaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Especialidades = _context.Especialidades.OrderBy(e => e.Nome).ToList();
                return View(model);
            }

            var novaCategoria = new Categoria
            {
                EspecialidadeId = model.EspecialidadeId,
                Nome = model.Nome,
                Descricao = model.Descricao,
                Ativo = true
            };

            _context.Categorias.Add(novaCategoria);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Update(int id)
        {
            var categoria = _context.Categorias.Find(id);
            if (categoria == null) return NotFound();

            ViewBag.Especialidades = _context.Especialidades.OrderBy(e => e.Nome).ToList();

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
                ViewBag.Especialidades = _context.Especialidades.OrderBy(e => e.Nome).ToList();
                return View(model);
            }

            var categoria = _context.Categorias.Find(model.Id);
            if (categoria == null) return NotFound();

            categoria.EspecialidadeId = model.EspecialidadeId;
            categoria.Nome = model.Nome;
            categoria.Descricao = model.Descricao;

            _context.Categorias.Update(categoria);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var categoria = _context.Categorias
                .Include(c => c.Especialidades)
                .FirstOrDefault(c => c.Id == id);

            if (categoria == null) return NotFound();

            return View(categoria);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var categoria = _context.Categorias.Find(id);
            if (categoria == null) return NotFound();

            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var categoria = _context.Categorias.IgnoreQueryFilters().FirstOrDefault(c => c.Id == id);
            if (categoria == null) return NotFound();

            categoria.Ativo = true;

            _context.Categorias.Update(categoria);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
