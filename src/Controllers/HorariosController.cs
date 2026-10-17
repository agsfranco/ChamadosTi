using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.ViewModels.Horario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class HorariosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<HorariosController> _localizer;

        public HorariosController(AppDbContext context, IStringLocalizer<HorariosController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(bool exibirInativos = false)
        {
            List<Horario> horarios;

            if (exibirInativos)
            {
                horarios = await _context.Horarios
                    .IgnoreQueryFilters()
                    .Include(h => h.SetoresHorarios!)
                        .ThenInclude(sh => sh.Setor)
                    .Where(h => !h.Ativo)
                    .OrderBy(h => h.DiaDaSemana)
                    .ToListAsync();
            }
            else
            {
                horarios = await _context.Horarios
                    .Include(h => h.SetoresHorarios!)
                        .ThenInclude(sh => sh.Setor)
                    .OrderBy(h => h.DiaDaSemana)
                    .ToListAsync();
            }

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

            if (model.JornadaFim <= model.JornadaInicio || model.AlmocoFim <= model.AlmocoInicio)
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

            _context.Horarios.Add(novoHorario);
            await _context.SaveChangesAsync();

            if (model.SetoresIdsSelecionados != null && model.SetoresIdsSelecionados.Any())
            {
                foreach (var setorId in model.SetoresIdsSelecionados)
                {
                    _context.SetoresHorarios.Add(new SetorHorario
                    {
                        HorarioId = novoHorario.Id,
                        SetorId = setorId,
                        Ativo = true
                    });
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var horario = await BuscarHorarioPorIdAsync(id, false, true);
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

            var horario = await BuscarHorarioPorIdAsync(model.Id, false, true);
            if (horario == null) return NotFound();

            if (model.JornadaFim <= model.JornadaInicio || model.AlmocoFim <= model.AlmocoInicio)
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

            if (horario.SetoresHorarios != null)
            {
                _context.SetoresHorarios.RemoveRange(horario.SetoresHorarios);
            }

            if (model.SetoresIdsSelecionados != null && model.SetoresIdsSelecionados.Any())
            {
                foreach (var setorId in model.SetoresIdsSelecionados)
                {
                    _context.SetoresHorarios.Add(new SetorHorario
                    {
                        HorarioId = horario.Id,
                        SetorId = setorId,
                        Ativo = true
                    });
                }
            }

            _context.Horarios.Update(horario);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var horario = await BuscarHorarioPorIdAsync(id, false, false);
            if (horario == null) return NotFound();

            return View(horario);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var horario = await BuscarHorarioPorIdAsync(id, false, true);
            if (horario == null) return NotFound();

            if (horario.SetoresHorarios != null)
            {
                _context.SetoresHorarios.RemoveRange(horario.SetoresHorarios);
            }

            _context.Horarios.Remove(horario);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var horario = await BuscarHorarioPorIdAsync(id, true, false);
            if (horario == null) return NotFound();

            horario.Ativo = true;
            _context.Horarios.Update(horario);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task CarregaSetoresAsync()
        {
            ViewBag.Setores = await _context.Setores.OrderBy(s => s.Nome).ToListAsync();
        }

        private async Task<Horario?> BuscarHorarioPorIdAsync(int id, bool ignorarFiltros, bool incluirSetores)
        {
            var query = _context.Horarios.AsQueryable();

            if (ignorarFiltros) query = query.IgnoreQueryFilters();
            if (incluirSetores) query = query.Include(h => h.SetoresHorarios);

            return await query.FirstOrDefaultAsync(h => h.Id == id);
        }
    }
}
