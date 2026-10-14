using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.ViewModels.Item;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize]
    public class ItensController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<ItensController> _localizer;

        public ItensController(AppDbContext context, IStringLocalizer<ItensController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public IActionResult Index(bool exibirInativos = false)
        {
            List<Item> itens;

            if (exibirInativos)
            {
                itens = _context.Itens
                    .IgnoreQueryFilters()
                    .Include(i => i.Tipos)
                    .Include(i => i.Marcas)
                    .Include(i => i.Setores)
                    .Where(i => !i.Ativo)
                    .OrderBy(i => i.Nome)
                    .ToList();
            }
            else
            {
                itens = _context.Itens
                    .Include(i => i.Tipos)
                    .Include(i => i.Marcas)
                    .Include(i => i.Setores)
                    .OrderBy(i => i.Nome)
                    .ToList();
            }

            ViewBag.ExibirInativos = exibirInativos;
            return View(itens);
        }

        private void CarregaDropdowns()
        {
            ViewBag.Tipos = _context.Tipos.OrderBy(t => t.Nome).ToList();
            ViewBag.Marcas = _context.Marcas.OrderBy(m => m.Nome).ToList();
            ViewBag.Setores = _context.Setores.OrderBy(s => s.Nome).ToList();
        }

        [HttpGet]
        public IActionResult Create()
        {
            CarregaDropdowns();
            return View(new ItemViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                CarregaDropdowns();
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
        public IActionResult Update(int id)
        {
            var item = _context.Itens.Find(id);
            if (item == null) return NotFound();

            CarregaDropdowns();

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
                CarregaDropdowns();
                return View(model);
            }

            var item = _context.Itens.Find(model.Id);
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
        public IActionResult Delete(int id)
        {
            var item = _context.Itens
                .Include(i => i.Tipos)
                .Include(i => i.Marcas)
                .Include(i => i.Setores)
                .FirstOrDefault(i => i.Id == id);

            if (item == null) return NotFound();

            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = _context.Itens.Find(id);
            if (item == null) return NotFound();

            _context.Itens.Remove(item); // O SaveChanges interceptará e aplicará o Soft Delete padrão
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var item = _context.Itens.IgnoreQueryFilters().FirstOrDefault(i => i.Id == id);
            if (item == null) return NotFound();

            item.Ativo = true;

            _context.Itens.Update(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
