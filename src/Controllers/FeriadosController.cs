using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.ViewModels.Feriado;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FeriadosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<FeriadosController> _localizer;

        public FeriadosController(AppDbContext context, IStringLocalizer<FeriadosController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(bool exibirInativos = false)
        {
            List<Feriado> feriados;

            if (exibirInativos)
            {
                feriados = await _context.Feriados
                    .IgnoreQueryFilters()
                    .Where(f => !f.Ativo)
                    .OrderBy(f => f.Dia)
                    .ToListAsync();
            }
            else
            {
                feriados = await _context.Feriados
                    .OrderBy(f => f.Dia)
                    .ToListAsync();
            }

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

            if (await ValidarFeriadoDuplicadoAsync(model.Dia!.Value, model.Id))
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

            _context.Feriados.Add(novoFeriado);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var feriado = await BuscarFeriadoPorIdAsync(id, false);
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

            var feriado = await BuscarFeriadoPorIdAsync(model.Id, false);
            if (feriado == null) return NotFound();

            if (await ValidarFeriadoDuplicadoAsync(model.Dia!.Value, model.Id))
            {
                ModelState.AddModelError("Dia", _localizer["Error_FeriadoDuplicado"]);
                return View(model);
            }

            feriado.Nome = model.Nome;
            feriado.Dia = model.Dia!.Value;

            _context.Feriados.Update(feriado);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var feriado = await BuscarFeriadoPorIdAsync(id, false);
            if (feriado == null) return NotFound();

            return View(feriado);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var feriado = await BuscarFeriadoPorIdAsync(id, false);
            if (feriado == null) return NotFound();

            _context.Feriados.Remove(feriado);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var feriado = await BuscarFeriadoPorIdAsync(id, true);
            if (feriado == null) return NotFound();

            feriado.Ativo = true;

            _context.Feriados.Update(feriado);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<Feriado?> BuscarFeriadoPorIdAsync(int id, bool ignorarFiltros)
        {
            var query = _context.Feriados.AsQueryable();

            if (ignorarFiltros) query = query.IgnoreQueryFilters();

            return await query.FirstOrDefaultAsync(f => f.Id == id);
        }

        private async Task<bool> ValidarFeriadoDuplicadoAsync(DateOnly dia, int id)
        {
            return await _context.Feriados.AnyAsync(f => f.Dia == dia && f.Id != id);
        }
    }
}
