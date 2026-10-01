using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.ViewModels.Especialidade;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize]
    public class EspecialidadesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<EspecialidadesController> _localizer;

        public EspecialidadesController(AppDbContext context, IStringLocalizer<EspecialidadesController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public IActionResult Index(bool exibirInativos = false)
        {
            List<Especialidade> especialidades;

            if (exibirInativos)
            {
                // Aplica IgnoreQueryFilters() para ignorar o soft delete e busca APENAS os inativos
                especialidades = _context.Especialidades.IgnoreQueryFilters().Where(e => !e.Ativo).ToList();
            }
            else
            {
                // Query Filter padrão traz apenas ativos
                especialidades = _context.Especialidades.ToList();
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
        public IActionResult Update(int id)
        {
            var esp = _context.Especialidades.Find(id);
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

            var esp = _context.Especialidades.Find(model.Id);
            if (esp == null) return NotFound();

            esp.Nome = model.Nome;
            esp.Descricao = model.Descricao;

            _context.Especialidades.Update(esp);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var esp = _context.Especialidades.Find(id);
            if (esp == null) return NotFound();

            return View(esp);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var esp = _context.Especialidades.Find(id);
            if (esp == null) return NotFound();

            _context.Especialidades.Remove(esp); // Aciona o interceptador do seu DbContext
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var esp = _context.Especialidades.IgnoreQueryFilters().FirstOrDefault(e => e.Id == id);
            if (esp == null) return NotFound();

            esp.Ativo = true;

            _context.Especialidades.Update(esp);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
