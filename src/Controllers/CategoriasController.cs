using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.ViewModels.Categoria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoriasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<CategoriasController> _localizer;

        public CategoriasController(AppDbContext context, IStringLocalizer<CategoriasController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(bool exibirInativos = false)
        {
            List<Categoria> categorias;

            if (exibirInativos)
            {
                categorias = await _context.Categorias
                    .IgnoreQueryFilters()
                    .Include(c => c.Especialidades)
                    .Where(c => !c.Ativo)
                    .OrderBy(c => c.Nome)
                    .ToListAsync();
            }
            else
            {
                categorias = await _context.Categorias
                    .Include(c => c.Especialidades)
                    .OrderBy(c => c.Nome)
                    .ToListAsync();
            }

            ViewBag.ExibirInativos = exibirInativos;
            return View(categorias);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Especialidades = await CarregarEspecialidadesFormularioAsync();
            return View(new CategoriaViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoriaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Especialidades = await CarregarEspecialidadesFormularioAsync();
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
        public async Task<IActionResult> Update(int id)
        {
            var categoria = await BuscarCategoriaPorIdAsync(id, false, false);
            if (categoria == null) return NotFound();

            ViewBag.Especialidades = await CarregarEspecialidadesFormularioAsync();

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
                ViewBag.Especialidades = await CarregarEspecialidadesFormularioAsync();
                return View(model);
            }

            var categoria = await BuscarCategoriaPorIdAsync(model.Id, false, false);
            if (categoria == null) return NotFound();

            categoria.EspecialidadeId = model.EspecialidadeId;
            categoria.Nome = model.Nome;
            categoria.Descricao = model.Descricao;

            _context.Categorias.Update(categoria);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var categoria = await BuscarCategoriaPorIdAsync(id, false, true);
            if (categoria == null) return NotFound();

            return View(categoria);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var categoria = await BuscarCategoriaPorIdAsync(id, false, false);
            if (categoria == null) return NotFound();

            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var categoria = await BuscarCategoriaPorIdAsync(id, true, false);
            if (categoria == null) return NotFound();

            categoria.Ativo = true;

            _context.Categorias.Update(categoria);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<Categoria?> BuscarCategoriaPorIdAsync(int id, bool ignorarFiltros, bool incluirEspecialidades)
        {
            var query = _context.Categorias.AsQueryable();
            
            if (ignorarFiltros) query = query.IgnoreQueryFilters();
            if (incluirEspecialidades) query = query.Include(c => c.Especialidades);
            
            return await query.FirstOrDefaultAsync(c => c.Id == id);
        }

        private async Task<List<Especialidade>> CarregarEspecialidadesFormularioAsync()
        {
            return await _context.Especialidades.OrderBy(e => e.Nome).ToListAsync();
        }
    }
}
