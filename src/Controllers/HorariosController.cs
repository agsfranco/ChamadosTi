using ChamadosTi.Models;
using ChamadosTi.Services.Interfaces;
using ChamadosTi.ViewModels.Horario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class HorariosController : Controller
    {
        private readonly IHorarioService _horarioService;
        private readonly ISetorService _setorService;
        private readonly IStringLocalizer<HorariosController> _localizer;

        public HorariosController(IHorarioService horarioService, ISetorService setorService, IStringLocalizer<HorariosController> localizer)
        {
            _horarioService = horarioService;
            _setorService = setorService;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(bool exibirInativos = false)
        {
            var horarios = await _horarioService.ListarAsync(exibirInativos);

            ViewBag.ExibirInativos = exibirInativos;
            return View(horarios);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await CarregaSetoresAsync();
            return View(new HorarioViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HorarioViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CarregaSetoresAsync();
                return View(model);
            }

            if (!_horarioService.JornadaValida(model.JornadaInicio, model.JornadaFim, model.AlmocoInicio, model.AlmocoFim))
            {
                ModelState.AddModelError("", _localizer["Error_HorarioInvalido"]);
                await CarregaSetoresAsync();
                return View(model);
            }

            var novoHorario = new Horario
            {
                DiaDaSemana = model.DiaDaSemana,
                JornadaInicio = model.JornadaInicio!.Value,
                JornadaFim = model.JornadaFim!.Value,
                AlmocoInicio = model.AlmocoInicio,
                AlmocoFim = model.AlmocoFim,
                Ativo = true
            };

            await _horarioService.CriarComSetoresAsync(novoHorario, model.SetoresIdsSelecionados);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var horario = await _horarioService.ObterPorIdAsync(id, incluirRelacionamentos: true);
            if (horario == null) return NotFound();

            await CarregaSetoresAsync();

            var model = new HorarioViewModel
            {
                Id = horario.Id,
                DiaDaSemana = horario.DiaDaSemana,
                JornadaInicio = horario.JornadaInicio,
                JornadaFim = horario.JornadaFim,
                AlmocoInicio = horario.AlmocoInicio,
                AlmocoFim = horario.AlmocoFim,
                SetoresIdsSelecionados = horario.SetoresHorarios?.Select(sh => sh.SetorId).ToList() ?? new List<int>()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(HorarioViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CarregaSetoresAsync();
                return View(model);
            }

            var horario = await _horarioService.ObterPorIdAsync(model.Id, incluirRelacionamentos: true);
            if (horario == null) return NotFound();

            if (!_horarioService.JornadaValida(model.JornadaInicio, model.JornadaFim, model.AlmocoInicio, model.AlmocoFim))
            {
                ModelState.AddModelError("", _localizer["Error_HorarioInvalido"]);
                await CarregaSetoresAsync();
                return View(model);
            }

            horario.DiaDaSemana = model.DiaDaSemana;
            horario.JornadaInicio = model.JornadaInicio!.Value;
            horario.JornadaFim = model.JornadaFim!.Value;
            horario.AlmocoInicio = model.AlmocoInicio!.Value;
            horario.AlmocoFim = model.AlmocoFim!.Value;

            await _horarioService.AtualizarComSetoresAsync(horario, model.SetoresIdsSelecionados);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var horario = await _horarioService.ObterPorIdAsync(id);
            if (horario == null) return NotFound();

            return View(horario);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var horario = await _horarioService.ObterPorIdAsync(id, incluirRelacionamentos: true);
            if (horario == null) return NotFound();

            await _horarioService.RemoverComSetoresAsync(horario);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var horario = await _horarioService.ObterPorIdAsync(id, ignorarFiltros: true);
            if (horario == null) return NotFound();

            await _horarioService.AtivarAsync(horario);
            return RedirectToAction(nameof(Index));
        }

        private async Task CarregaSetoresAsync()
        {
            ViewBag.Setores = await _setorService.ListarOrdenadosPorNomeAsync();
        }
    }
}
