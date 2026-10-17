using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.ViewModels.Prioridade;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PrioridadesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<PrioridadesController> _localizer;

        public PrioridadesController(AppDbContext context, IStringLocalizer<PrioridadesController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(bool exibirInativos = false)
        {
            List<Prioridade> prioridades;

            if (exibirInativos)
            {
                prioridades = await _context.Prioridades
                    .IgnoreQueryFilters()
                    .Where(p => !p.Ativo)
                    .OrderBy(p => p.Nome)
                    .ToListAsync();
            }
            else
            {
                prioridades = await _context.Prioridades
                    .OrderBy(p => p.Nome)
                    .ToListAsync();
            }

            ViewBag.ExibirInativos = exibirInativos;
            return View(prioridades);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new PrioridadeViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PrioridadeViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await ValidarPadraoDuplicadoAsync(model.Id, model.Padrao))
            {
                ModelState.AddModelError("Padrao", _localizer["Error_PadraoJaExistente"]);
                return View(model);
            }

            var novaPrioridade = new Prioridade
            {
                Nome = model.Nome,
                TempoRespostaPadrao = model.TempoRespostaPadrao,
                TempoResolucaoPadrao = model.TempoResolucaoPadrao,
                Padrao = model.Padrao,
                Ativo = true
            };

            _context.Prioridades.Add(novaPrioridade);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var prioridade = await BuscarPrioridadePorIdAsync(id, false);
            if (prioridade == null) return NotFound();

            var model = new PrioridadeViewModel
            {
                Id = prioridade.Id,
                Nome = prioridade.Nome,
                TempoRespostaPadrao = prioridade.TempoRespostaPadrao,
                TempoResolucaoPadrao = prioridade.TempoResolucaoPadrao,
                Padrao = prioridade.Padrao
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(PrioridadeViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await ValidarPadraoDuplicadoAsync(model.Id, model.Padrao))
            {
                ModelState.AddModelError("Padrao", _localizer["Error_PadraoJaExistente"]);
                return View(model);
            }            

            var prioridade = await BuscarPrioridadePorIdAsync(model.Id, false);
            if (prioridade == null) return NotFound();

            prioridade.Nome = model.Nome;
            prioridade.TempoRespostaPadrao = model.TempoRespostaPadrao;
            prioridade.TempoResolucaoPadrao = model.TempoResolucaoPadrao;
            prioridade.Padrao = model.Padrao;

            _context.Prioridades.Update(prioridade);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var prioridade = await BuscarPrioridadePorIdAsync(id, false);
            if (prioridade == null) return NotFound();

            return View(prioridade);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var prioridade = await BuscarPrioridadePorIdAsync(id, false);
            if (prioridade == null) return NotFound();

            _context.Prioridades.Remove(prioridade);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var prioridade = await BuscarPrioridadePorIdAsync(id, true);
            if (prioridade == null) return NotFound();

            prioridade.Ativo = true;

            _context.Prioridades.Update(prioridade);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<Prioridade?> BuscarPrioridadePorIdAsync(int id, bool ignorarFiltros)
        {
            var query = _context.Prioridades.AsQueryable();
            if (ignorarFiltros) query = query.IgnoreQueryFilters();
            return await query.FirstOrDefaultAsync(p => p.Id == id);
        }

        private async Task<bool> ValidarPadraoDuplicadoAsync(int id, bool ehPadrao)
        {
            if (!ehPadrao) return false;
            return await _context.Prioridades.AnyAsync(x => x.Id != id && x.Padrao && x.Ativo);
        }
    }
}
