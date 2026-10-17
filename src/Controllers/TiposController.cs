using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.ViewModels.Tipo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChamadosTi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TiposController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<TiposController> _localizer;

        public TiposController(AppDbContext context, IStringLocalizer<TiposController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(bool exibirInativos = false)
        {
            List<Tipo> tipos;

            if (exibirInativos)
            {
                tipos = await _context.Tipos
                                      .IgnoreQueryFilters()
                                      .Where(t => !t.Ativo)
                                      .OrderBy(t => t.Nome)
                                      .ToListAsync();
            }
            else
            {
                tipos = await _context.Tipos
                                      .OrderBy(t => t.Nome)
                                      .ToListAsync();
            }

            ViewBag.ExibirInativos = exibirInativos;
            return View(tipos);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new TipoViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TipoViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var novoTipo = new Tipo
            {
                Nome = model.Nome,
                Comentario = model.Comentario,
                Ativo = true
            };

            _context.Tipos.Add(novoTipo);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var tipo = await BuscarTipoPorIdAsync(id);
            if (tipo == null) return NotFound();

            var model = new TipoViewModel
            {
                Id = tipo.Id,
                Nome = tipo.Nome,
                Comentario = tipo.Comentario
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(TipoViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var tipo = await BuscarTipoPorIdAsync(model.Id);
            if (tipo == null) return NotFound();

            tipo.Nome = model.Nome;
            tipo.Comentario = model.Comentario;

            _context.Tipos.Update(tipo);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var tipo = await BuscarTipoPorIdAsync(id);
            if (tipo == null) return NotFound();

            return View(tipo);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tipo = await BuscarTipoPorIdAsync(id);
            if (tipo == null) return NotFound();

            _context.Tipos.Remove(tipo);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var tipo = await BuscarTipoPorIdAsync(id, ignorarFiltrosGlobais: true);
            if (tipo == null) return NotFound();

            tipo.Ativo = true;

            _context.Tipos.Update(tipo);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<Tipo?> BuscarTipoPorIdAsync(int id, bool ignorarFiltrosGlobais = false)
        {
            var query = _context.Tipos.AsQueryable();

            if (ignorarFiltrosGlobais)
            {
                query = query.IgnoreQueryFilters();
            }

            return await query.FirstOrDefaultAsync(t => t.Id == id);
        }
    }
}
