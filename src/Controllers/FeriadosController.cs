using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.ViewModels.Feriado;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize]
    public class FeriadosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<FeriadosController> _localizer;

        public FeriadosController(AppDbContext context, IStringLocalizer<FeriadosController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public IActionResult Index(bool exibirInativos = false)
        {
            List<Feriado> feriados;

            if (exibirInativos)
            {
                feriados = _context.Feriados
                    .IgnoreQueryFilters()
                    .Where(f => !f.Ativo)
                    .OrderBy(f => f.Dia)
                    .ToList();
            }
            else
            {
                feriados = _context.Feriados
                    .OrderBy(f => f.Dia)
                    .ToList();
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

            // Validação extra: Impede cadastrar o mesmo feriado na mesma data duas vezes
            var feriadoDuplicado = _context.Feriados.Any(f => f.Dia == model.Dia!.Value);
            if (feriadoDuplicado)
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
        public IActionResult Update(int id)
        {
            var feriado = _context.Feriados.Find(id);
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

            var feriado = _context.Feriados.Find(model.Id);
            if (feriado == null) return NotFound();

            var feriadoDuplicado = _context.Feriados.Any(f => f.Dia == model.Dia!.Value && f.Id != model.Id);
            if (feriadoDuplicado)
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
        public IActionResult Delete(int id)
        {
            var feriado = _context.Feriados.Find(id);
            if (feriado == null) return NotFound();

            return View(feriado);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var feriado = _context.Feriados.Find(id);
            if (feriado == null) return NotFound();

            _context.Feriados.Remove(feriado);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var feriado = _context.Feriados.IgnoreQueryFilters().FirstOrDefault(f => f.Id == id);
            if (feriado == null) return NotFound();

            feriado.Ativo = true;

            _context.Feriados.Update(feriado);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}