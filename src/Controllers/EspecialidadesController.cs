using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.ViewModels.Especialidade;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EspecialidadesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<EspecialidadesController> _localizer;

        public EspecialidadesController(AppDbContext context, IStringLocalizer<EspecialidadesController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(bool exibirInativos = false)
        {
            List<Especialidade> especialidades;

            if (exibirInativos)
            {
                especialidades = await _context.Especialidades
                    .IgnoreQueryFilters()
                    .Where(e => !e.Ativo)
                    .ToListAsync();
            }
            else
            {
                especialidades = await _context.Especialidades.ToListAsync();
            }

            ViewBag.ExibirInativos = exibirInativos;
            return View(especialidades);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new EspecialidadeViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EspecialidadeViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var novaEspecialidade = new Especialidade
            {
                Nome = model.Nome,
                Descricao = model.Descricao,
                Ativo = true
            };

            _context.Especialidades.Add(novaEspecialidade);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var esp = await BuscarEspecialidadePorIdAsync(id, false);
            if (esp == null) return NotFound();

            var model = new EspecialidadeViewModel
            {
                Id = esp.Id,
                Nome = esp.Nome,
                Descricao = esp.Descricao
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(EspecialidadeViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var esp = await BuscarEspecialidadePorIdAsync(model.Id, false);
            if (esp == null) return NotFound();

            esp.Nome = model.Nome;
            esp.Descricao = model.Descricao;

            _context.Especialidades.Update(esp);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var esp = await BuscarEspecialidadePorIdAsync(id, false);
            if (esp == null) return NotFound();

            return View(esp);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var esp = await BuscarEspecialidadePorIdAsync(id, false);
            if (esp == null) return NotFound();

            _context.Especialidades.Remove(esp);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var esp = await BuscarEspecialidadePorIdAsync(id, true);
            if (esp == null) return NotFound();

            esp.Ativo = true;

            _context.Especialidades.Update(esp);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<Especialidade?> BuscarEspecialidadePorIdAsync(int id, bool ignorarFiltros)
        {
            var query = _context.Especialidades.AsQueryable();

            if (ignorarFiltros) query = query.IgnoreQueryFilters();

            return await query.FirstOrDefaultAsync(e => e.Id == id);
        }
    }
}
