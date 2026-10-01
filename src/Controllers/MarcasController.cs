using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.ViewModels.Marca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize]
    public class MarcasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<MarcasController> _localizer;

        public MarcasController(AppDbContext context, IStringLocalizer<MarcasController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public IActionResult Index(bool exibirInativos = false)
        {
            List<Marca> marcas;

            if (exibirInativos)
            {
                marcas = _context.Marcas.IgnoreQueryFilters().Where(m => !m.Ativo).OrderBy(m => m.Nome).ToList();
            }
            else
            {
                marcas = _context.Marcas.OrderBy(m => m.Nome).ToList();
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
        public IActionResult Update(int id)
        {
            var marca = _context.Marcas.Find(id);
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

            var marca = _context.Marcas.Find(model.Id);
            if (marca == null) return NotFound();

            marca.Nome = model.Nome;

            _context.Marcas.Update(marca);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var marca = _context.Marcas.Find(id);
            if (marca == null) return NotFound();

            return View(marca);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var marca = _context.Marcas.Find(id);
            if (marca == null) return NotFound();

            _context.Marcas.Remove(marca);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var marca = _context.Marcas.IgnoreQueryFilters().FirstOrDefault(m => m.Id == id);
            if (marca == null) return NotFound();

            marca.Ativo = true;

            _context.Marcas.Update(marca);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
