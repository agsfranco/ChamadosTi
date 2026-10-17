using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.ViewModels.Item;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ItensController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<ItensController> _localizer;

        public ItensController(AppDbContext context, IStringLocalizer<ItensController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(bool exibirInativos = false)
        {
            List<Item> itens;

            if (exibirInativos)
            {
                itens = await _context.Itens
                    .IgnoreQueryFilters()
                    .Include(i => i.Tipos)
                    .Include(i => i.Marcas)
                    .Include(i => i.Setores)
                    .Where(i => !i.Ativo)
                    .OrderBy(i => i.Nome)
                    .ToListAsync();
            }
            else
            {
                itens = await _context.Itens
                    .Include(i => i.Tipos)
                    .Include(i => i.Marcas)
                    .Include(i => i.Setores)
                    .OrderBy(i => i.Nome)
                    .ToListAsync();
            }

            ViewBag.ExibirInativos = exibirInativos;
            return View(itens);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await CarregaDropdownsAsync();
            return View(new ItemViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CarregaDropdownsAsync();
                return View(model);
            }

            var novoItem = new Item
            {
                TipoId = model.TipoId!.Value,
                MarcaId = model.MarcaId!.Value,
                SetorId = model.SetorId,
                Nome = model.Nome,
                Comentario = model.Comentario,
                CriadoEm = DateTime.UtcNow,
                Ativo = true
            };

            _context.Itens.Add(novoItem);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var item = await BuscarItemPorIdAsync(id, false, false);
            if (item == null) return NotFound();

            await CarregaDropdownsAsync();

            var model = new ItemViewModel
            {
                Id = item.Id,
                TipoId = item.TipoId,
                MarcaId = item.MarcaId,
                SetorId = item.SetorId,
                Nome = item.Nome,
                Comentario = item.Comentario
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CarregaDropdownsAsync();
                return View(model);
            }

            var item = await BuscarItemPorIdAsync(model.Id, false, false);
            if (item == null) return NotFound();

            item.TipoId = model.TipoId!.Value;
            item.MarcaId = model.MarcaId!.Value;
            item.SetorId = model.SetorId;
            item.Nome = model.Nome;
            item.Comentario = model.Comentario;

            _context.Itens.Update(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await BuscarItemPorIdAsync(id, false, true);
            if (item == null) return NotFound();

            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await BuscarItemPorIdAsync(id, false, false);
            if (item == null) return NotFound();

            _context.Itens.Remove(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var item = await BuscarItemPorIdAsync(id, true, false);
            if (item == null) return NotFound();

            item.Ativo = true;

            _context.Itens.Update(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task CarregaDropdownsAsync()
        {
            ViewBag.Tipos = await _context.Tipos.OrderBy(t => t.Nome).ToListAsync();
            ViewBag.Marcas = await _context.Marcas.OrderBy(m => m.Nome).ToListAsync();
            ViewBag.Setores = await _context.Setores.OrderBy(s => s.Nome).ToListAsync();
        }

        private async Task<Item?> BuscarItemPorIdAsync(int id, bool ignorarFiltros, bool incluirRelacionamentos)
        {
            var query = _context.Itens.AsQueryable();

            if (ignorarFiltros) query = query.IgnoreQueryFilters();
            
            if (incluirRelacionamentos)
            {
                query = query.Include(i => i.Tipos)
                             .Include(i => i.Marcas)
                             .Include(i => i.Setores);
            }

            return await query.FirstOrDefaultAsync(i => i.Id == id);
        }
    }
}
