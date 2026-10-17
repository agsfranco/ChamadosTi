using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.ViewModels.DadoTecnico;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DadosTecnicosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<DadosTecnicosController> _localizer;

        public DadosTecnicosController(AppDbContext context, IStringLocalizer<DadosTecnicosController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(bool exibirInativos = false)
        {
            List<DadoTecnico> dadosTecnicos;

            if (exibirInativos)
            {
                dadosTecnicos = await _context.DadosTecnicos
                    .IgnoreQueryFilters()
                    .Include(d => d.Especialidades)
                    .Where(d => !d.Ativo)
                    .ToListAsync();
            }
            else
            {
                dadosTecnicos = await _context.DadosTecnicos
                    .Include(d => d.Especialidades)
                    .ToListAsync();
            }

            ViewBag.ExibirInativos = exibirInativos;
            return View(dadosTecnicos);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Especialidades = await CarregarEspecialidadesFormularioAsync();
            return View(new DadoTecnicoViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DadoTecnicoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Especialidades = await CarregarEspecialidadesFormularioAsync();
                return View(model);
            }

            var novoDado = new DadoTecnico
            {
                EspecialidadeId = model.EspecialidadeId,
                Nivel = model.Nivel,
                Ativo = true
            };

            _context.DadosTecnicos.Add(novoDado);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var dado = await BuscarDadoTecnicoPorIdAsync(id, false, false);
            if (dado == null) return NotFound();

            ViewBag.Especialidades = await CarregarEspecialidadesFormularioAsync();

            var model = new DadoTecnicoViewModel
            {
                Id = dado.Id,
                EspecialidadeId = dado.EspecialidadeId,
                Nivel = dado.Nivel
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(DadoTecnicoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Especialidades = await CarregarEspecialidadesFormularioAsync();
                return View(model);
            }

            var dado = await BuscarDadoTecnicoPorIdAsync(model.Id, false, false);
            if (dado == null) return NotFound();

            dado.EspecialidadeId = model.EspecialidadeId;
            dado.Nivel = model.Nivel;

            _context.DadosTecnicos.Update(dado);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var dado = await BuscarDadoTecnicoPorIdAsync(id, false, true);
            if (dado == null) return NotFound();

            return View(dado);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dado = await BuscarDadoTecnicoPorIdAsync(id, false, false);
            if (dado == null) return NotFound();

            _context.DadosTecnicos.Remove(dado);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var dado = await BuscarDadoTecnicoPorIdAsync(id, true, false);
            if (dado == null) return NotFound();

            dado.Ativo = true;

            _context.DadosTecnicos.Update(dado);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<DadoTecnico?> BuscarDadoTecnicoPorIdAsync(int id, bool ignorarFiltros, bool incluirEspecialidades)
        {
            var query = _context.DadosTecnicos.AsQueryable();

            if (ignorarFiltros) query = query.IgnoreQueryFilters();
            if (incluirEspecialidades) query = query.Include(d => d.Especialidades);

            return await query.FirstOrDefaultAsync(d => d.Id == id);
        }

        private async Task<List<Especialidade>> CarregarEspecialidadesFormularioAsync()
        {
            return await _context.Especialidades.OrderBy(e => e.Nome).ToListAsync();
        }
    }
}
