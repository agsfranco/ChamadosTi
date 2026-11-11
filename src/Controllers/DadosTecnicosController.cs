using ChamadosTi.Models;
using ChamadosTi.Services.Interfaces;
using ChamadosTi.ViewModels.DadoTecnico;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DadosTecnicosController : Controller
    {
        private readonly IDadoTecnicoService _dadoTecnicoService;
        private readonly IEspecialidadeService _especialidadeService;
        private readonly IStringLocalizer<DadosTecnicosController> _localizer;

        public DadosTecnicosController(IDadoTecnicoService dadoTecnicoService, IEspecialidadeService especialidadeService, IStringLocalizer<DadosTecnicosController> localizer)
        {
            _dadoTecnicoService = dadoTecnicoService;
            _especialidadeService = especialidadeService;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(bool exibirInativos = false)
        {
            var dadosTecnicos = await _dadoTecnicoService.ListarAsync(exibirInativos);

            ViewBag.ExibirInativos = exibirInativos;
            return View(dadosTecnicos);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View(new DadoTecnicoViewModel { Especialidades = await _especialidadeService.ListarOrdenadosPorNomeAsync() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DadoTecnicoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Especialidades = await _especialidadeService.ListarOrdenadosPorNomeAsync();
                return View(model);
            }

            var novoDado = new DadoTecnico
            {
                EspecialidadeId = model.EspecialidadeId,
                Nivel = model.Nivel,
                Ativo = true
            };

            await _dadoTecnicoService.CriarAsync(novoDado);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var dado = await _dadoTecnicoService.ObterPorIdAsync(id);
            if (dado == null) return NotFound();

            var especialidades = await _especialidadeService.ListarOrdenadosPorNomeAsync();

            var model = new DadoTecnicoViewModel
            {
                Especialidades = especialidades,
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
                model.Especialidades = await _especialidadeService.ListarOrdenadosPorNomeAsync();
                return View(model);
            }

            var dado = await _dadoTecnicoService.ObterPorIdAsync(model.Id);
            if (dado == null) return NotFound();

            dado.EspecialidadeId = model.EspecialidadeId;
            dado.Nivel = model.Nivel;

            await _dadoTecnicoService.AtualizarAsync(dado);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var dado = await _dadoTecnicoService.ObterPorIdAsync(id, incluirRelacionamentos: true);
            if (dado == null) return NotFound();

            return View(dado);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dado = await _dadoTecnicoService.ObterPorIdAsync(id);
            if (dado == null) return NotFound();

            await _dadoTecnicoService.RemoverAsync(dado);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var dado = await _dadoTecnicoService.ObterPorIdAsync(id, ignorarFiltros: true);
            if (dado == null) return NotFound();

            await _dadoTecnicoService.AtivarAsync(dado);
            return RedirectToAction(nameof(Index));
        }
    }
}
