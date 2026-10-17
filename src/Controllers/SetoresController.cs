using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.ViewModels.Setor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SetoresController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<SetoresController> _localizer;

        public SetoresController(AppDbContext context, IStringLocalizer<SetoresController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(bool exibirInativos = false)
        {
            List<Setor> setores;

            if (exibirInativos)
            {
                setores = await _context.Setores
                    .IgnoreQueryFilters()
                    .Where(s => !s.Ativo)
                    .ToListAsync();
            }
            else
            {
                setores = await _context.Setores.ToListAsync();
            }

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
        public async Task<IActionResult> Create(SetorViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await ValidarTiDuplicadoAsync(model.Id, model.Ti))
            {
                ModelState.AddModelError("Ti", _localizer["Error_TiJaExistente"]);
                return View(model);
            } 

            var novoSetor = new Setor
            {
                Nome = model.Nome,
                Descricao = model.Descricao,
                Ti = model.Ti,
                Ativo = true
            };

            _context.Setores.Add(novoSetor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var setor = await BuscarSetorPorIdAsync(id, false);
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

            if (await ValidarTiDuplicadoAsync(model.Id, model.Ti))
            {
                ModelState.AddModelError("Ti", _localizer["Error_TiJaExistente"]);
                return View(model);
            } 

            var setor = await BuscarSetorPorIdAsync(model.Id, false);
            if (setor == null) return NotFound();

            setor.Nome = model.Nome;
            setor.Descricao = model.Descricao;
            setor.Ti = model.Ti;

            _context.Setores.Update(setor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var setor = await BuscarSetorPorIdAsync(id, false);
            if (setor == null) return NotFound();

            return View(setor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var setor = await BuscarSetorPorIdAsync(id, false);
            if (setor == null) return NotFound();

            _context.Setores.Remove(setor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var setor = await BuscarSetorPorIdAsync(id, true);
            if (setor == null) return NotFound();

            setor.Ativo = true;

            _context.Setores.Update(setor);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task<Setor?> BuscarSetorPorIdAsync(int id, bool ignorarFiltros)
        {
            var query = _context.Setores.AsQueryable();
            if (ignorarFiltros) query = query.IgnoreQueryFilters();
            return await query.FirstOrDefaultAsync(s => s.Id == id);
        }

        private async Task<bool> ValidarTiDuplicadoAsync(int id, bool ehTi)
        {
            if (!ehTi) return false;
            return await _context.Setores.AnyAsync(x => x.Id != id && x.Ti && x.Ativo);
        }
    }
}
