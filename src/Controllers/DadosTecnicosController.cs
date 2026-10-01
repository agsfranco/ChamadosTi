using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.ViewModels.DadoTecnico;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize]
    public class DadosTecnicosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<DadosTecnicosController> _localizer;

        public DadosTecnicosController(AppDbContext context, IStringLocalizer<DadosTecnicosController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public IActionResult Index(bool exibirInativos = false)
        {
            List<DadoTecnico> dadosTecnicos;

            if (exibirInativos)
            {
                // Inclui a Especialidade mesmo para os registros inativos
                dadosTecnicos = _context.DadosTecnicos
                    .IgnoreQueryFilters()
                    .Include(d => d.Especialidades)
                    .Where(d => !d.Ativo)
                    .ToList();
            }
            else
            {
                // O Include traz os dados da Especialidade vinculada
                dadosTecnicos = _context.DadosTecnicos
                    .Include(d => d.Especialidades)
                    .ToList();
            }

            ViewBag.ExibirInativos = exibirInativos;
            return View(dadosTecnicos);
        }

        [HttpGet]
        public IActionResult Create()
        {
            // Carrega apenas as especialidades ativas para o Select
            ViewBag.Especialidades = _context.Especialidades.ToList();
            return View(new DadoTecnicoViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DadoTecnicoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Especialidades = _context.Especialidades.ToList();
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
        public IActionResult Update(int id)
        {
            var dado = _context.DadosTecnicos.Find(id);
            if (dado == null) return NotFound();

            ViewBag.Especialidades = _context.Especialidades.ToList();

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
                ViewBag.Especialidades = _context.Especialidades.ToList();
                return View(model);
            }

            var dado = _context.DadosTecnicos.Find(model.Id);
            if (dado == null) return NotFound();

            dado.EspecialidadeId = model.EspecialidadeId;
            dado.Nivel = model.Nivel;

            _context.DadosTecnicos.Update(dado);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var dado = _context.DadosTecnicos
                .Include(d => d.Especialidades)
                .FirstOrDefault(d => d.Id == id);

            if (dado == null) return NotFound();

            return View(dado);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dado = _context.DadosTecnicos.Find(id);
            if (dado == null) return NotFound();

            _context.DadosTecnicos.Remove(dado);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var dado = _context.DadosTecnicos.IgnoreQueryFilters().FirstOrDefault(d => d.Id == id);
            if (dado == null) return NotFound();

            dado.Ativo = true;

            _context.DadosTecnicos.Update(dado);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
