using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.ViewModels.Prioridade;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize]
    public class PrioridadesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<PrioridadesController> _localizer;

        public PrioridadesController(AppDbContext context, IStringLocalizer<PrioridadesController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        // GET: Prioridades ou Prioridades?exibirInativos=true
        public IActionResult Index(bool exibirInativos = false)
        {
            List<Prioridade> prioridades;

            if (exibirInativos)
            {
                // Busca apenas os registros inativos (ISoftDelete) ordenados por nome
                prioridades = _context.Prioridades
                    .IgnoreQueryFilters()
                    .Where(p => !p.Ativo)
                    .OrderBy(p => p.Nome)
                    .ToList();
            }
            else
            {
                // Filtro global traz apenas os ativos ordenados por nome
                prioridades = _context.Prioridades
                    .OrderBy(p => p.Nome)
                    .ToList();
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

            var jaExistePadrao = _context.Prioridades.Any(x => x.Id != model.Id && x.Padrao && x.Ativo);
            if (jaExistePadrao)
            {
                ModelState.AddModelError("Padrao", _localizer["Error_PadraoJaExistente"]);
                ViewBag.Prioridade = model;
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
        public IActionResult Update(int id)
        {
            var prioridade = _context.Prioridades.Find(id);
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

            var jaExistePadrao = _context.Prioridades.Any(x => x.Id != model.Id && x.Padrao && x.Ativo);
            if (jaExistePadrao)
            {
                ModelState.AddModelError("Padrao", _localizer["Error_PadraoJaExistente"]);
                ViewBag.Prioridade = model;
                return View(model);
            }            

            var prioridade = _context.Prioridades.Find(model.Id);
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
        public IActionResult Delete(int id)
        {
            var prioridade = _context.Prioridades.Find(id);
            if (prioridade == null) return NotFound();

            return View(prioridade);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var prioridade = _context.Prioridades.Find(id);
            if (prioridade == null) return NotFound();

            _context.Prioridades.Remove(prioridade); // Dispara o seu Soft Delete automático
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var prioridade = _context.Prioridades.IgnoreQueryFilters().FirstOrDefault(p => p.Id == id);
            if (prioridade == null) return NotFound();

            prioridade.Ativo = true;

            _context.Prioridades.Update(prioridade);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
