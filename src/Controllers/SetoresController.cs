using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.ViewModels.Setor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize]
    public class SetoresController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<SetoresController> _localizer;

        public SetoresController(AppDbContext context, IStringLocalizer<SetoresController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public IActionResult Index(bool exibirInativos = false)
        {
            List<Setor> setores;

            if (exibirInativos)
            {
                setores = _context.Setores.IgnoreQueryFilters().Where(s => !s.Ativo).ToList();
            }
            else
            {
                setores = _context.Setores.ToList();
            }

            ViewBag.ExibirInativos = exibirInativos;
            return View(setores);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new SetorViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SetorViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var novoSetor = new Setor
            {
                Nome = model.Nome,
                Descricao = model.Descricao,
                Ativo = true
            };

            _context.Setores.Add(novoSetor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Update(int id)
        {
            var setor = _context.Setores.Find(id);
            if (setor == null) return NotFound();

            var model = new SetorViewModel
            {
                Id = setor.Id,
                Nome = setor.Nome,
                Descricao = setor.Descricao
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(SetorViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var setor = _context.Setores.Find(model.Id);
            if (setor == null) return NotFound();

            setor.Nome = model.Nome;
            setor.Descricao = model.Descricao;

            _context.Setores.Update(setor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var setor = _context.Setores.Find(id);
            if (setor == null) return NotFound();

            return View(setor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var setor = _context.Setores.Find(id);
            if (setor == null) return NotFound();

            _context.Setores.Remove(setor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var setor = _context.Setores.IgnoreQueryFilters().FirstOrDefault(s => s.Id == id);
            if (setor == null) return NotFound();

            setor.Ativo = true;

            _context.Setores.Update(setor);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
