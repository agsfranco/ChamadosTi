using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.ViewModels.Marca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MarcasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<MarcasController> _localizer;

        public MarcasController(AppDbContext context, IStringLocalizer<MarcasController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(bool exibirInativos = false)
        {
            List<Marca> marcas;

            if (exibirInativos)
            {
                marcas = await _context.Marcas
                    .IgnoreQueryFilters()
                    .Where(m => !m.Ativo)
                    .OrderBy(m => m.Nome)
                    .ToListAsync();
            }
            else
            {
                marcas = await _context.Marcas
                    .OrderBy(m => m.Nome)
                    .ToListAsync();
            }

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

            _context.Marcas.Add(novaMarca);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var marca = await BuscarMarcaPorIdAsync(id, false);
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

            var marca = await BuscarMarcaPorIdAsync(model.Id, false);
            if (marca == null) return NotFound();

            marca.Nome = model.Nome;

            _context.Marcas.Update(marca);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var marca = await BuscarMarcaPorIdAsync(id, false);
            if (marca == null) return NotFound();

            return View(marca);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var marca = await BuscarMarcaPorIdAsync(id, false);
            if (marca == null) return NotFound();

            _context.Marcas.Remove(marca);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var marca = await BuscarMarcaPorIdAsync(id, true);
            if (marca == null) return NotFound();

            marca.Ativo = true;

            _context.Marcas.Update(marca);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<Marca?> BuscarMarcaPorIdAsync(int id, bool ignorarFiltros)
        {
            var query = _context.Marcas.AsQueryable();

            if (ignorarFiltros) query = query.IgnoreQueryFilters();

            return await query.FirstOrDefaultAsync(m => m.Id == id);
        }
    }
}
