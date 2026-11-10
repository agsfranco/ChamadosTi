using ChamadosTi.Models;
using ChamadosTi.Services.Interfaces;
using ChamadosTi.ViewModels.Especialidade;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EspecialidadesController : Controller
    {
        private readonly IEspecialidadeService _especialidadeService;
        private readonly IStringLocalizer<EspecialidadesController> _localizer;

        public EspecialidadesController(IEspecialidadeService especialidadeService, IStringLocalizer<EspecialidadesController> localizer)
        {
            _especialidadeService = especialidadeService;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(bool exibirInativos = false)
        {
            var especialidades = await _especialidadeService.ListarAsync(exibirInativos);

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

            await _especialidadeService.CriarAsync(novaEspecialidade);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var esp = await _especialidadeService.ObterPorIdAsync(id);
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

            var esp = await _especialidadeService.ObterPorIdAsync(model.Id);
            if (esp == null) return NotFound();

            esp.Nome = model.Nome;
            esp.Descricao = model.Descricao;

            await _especialidadeService.AtualizarAsync(esp);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var esp = await _especialidadeService.ObterPorIdAsync(id);
            if (esp == null) return NotFound();

            return View(esp);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var esp = await _especialidadeService.ObterPorIdAsync(id);
            if (esp == null) return NotFound();

            await _especialidadeService.RemoverAsync(esp);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var esp = await _especialidadeService.ObterPorIdAsync(id, ignorarFiltros: true);
            if (esp == null) return NotFound();

            await _especialidadeService.AtivarAsync(esp);
            return RedirectToAction(nameof(Index));
        }
    }
}
