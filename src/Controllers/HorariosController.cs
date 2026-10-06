using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.ViewModels.Horario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize]
    public class HorariosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<HorariosController> _localizer;

        public HorariosController(AppDbContext context, IStringLocalizer<HorariosController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public IActionResult Index(bool exibirInativos = false)
        {
            List<Horario> horarios;

            if (exibirInativos)
            {
                horarios = _context.Horarios
                    .IgnoreQueryFilters()
                    .Include(h => h.SetoresHorarios!)
                        .ThenInclude(sh => sh.Setor)
                    .Where(h => !h.Ativo)
                    .OrderBy(h => h.DiaDaSemana)
                    .ToList();
            }
            else
            {
                horarios = _context.Horarios
                    .Include(h => h.SetoresHorarios!)
                        .ThenInclude(sh => sh.Setor)
                    .OrderBy(h => h.DiaDaSemana)
                    .ToList();
            }

            ViewBag.ExibirInativos = exibirInativos;
            return View(horarios);
        }

        private void CarregaSetores()
        {
            ViewBag.Setores = _context.Setores.OrderBy(s => s.Nome).ToList();
        }

        [HttpGet]
        public IActionResult Create()
        {
            CarregaSetores();
            return View(new HorarioViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HorarioViewModel model)
        {
            if (!ModelState.IsValid)
            {
                CarregaSetores();
                return View(model);
            }

            // Validação lógica: O horário de fim não pode ser menor que o de início
            if (model.JornadaFim <= model.JornadaInicio || model.AlmocoFim <= model.AlmocoInicio)
            {
                ModelState.AddModelError("", _localizer["Error_HorarioInvalido"]);
                CarregaSetores();
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

            // Salva as associações muitos-para-muitos na tabela pivot
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
        public IActionResult Update(int id)
        {
            var horario = _context.Horarios
                .Include(h => h.SetoresHorarios)
                .FirstOrDefault(h => h.Id == id);

            if (horario == null) return NotFound();

            CarregaSetores();

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
                CarregaSetores();
                return View(model);
            }

            var horario = _context.Horarios.Include(h => h.SetoresHorarios).FirstOrDefault(h => h.Id == model.Id);
            if (horario == null) return NotFound();

            if (model.JornadaFim <= model.JornadaInicio || model.AlmocoFim <= model.AlmocoInicio)
            {
                ModelState.AddModelError("", _localizer["Error_HorarioInvalido"]);
                CarregaSetores();
                return View(model);
            }

            horario.DiaDaSemana = model.DiaDaSemana;
            horario.JornadaInicio = model.JornadaInicio!.Value;
            horario.JornadaFim = model.JornadaFim!.Value;
            horario.AlmocoInicio = model.AlmocoInicio!.Value;
            horario.AlmocoFim = model.AlmocoFim!.Value;

            // Atualização inteligente da tabela pivot: Limpa os vínculos antigos e reinsere os novos
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
        public IActionResult Delete(int id)
        {
            var horario = _context.Horarios.Find(id);
            if (horario == null) return NotFound();

            return View(horario);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var horario = _context.Horarios.Include(h => h.SetoresHorarios).FirstOrDefault(h => h.Id == id);
            if (horario == null) return NotFound();

            // Ao inativar o horário principal, cascateia a inativação para a tabela de ligação pivot
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
            var horario = _context.Horarios.IgnoreQueryFilters().FirstOrDefault(h => h.Id == id);
            if (horario == null) return NotFound();

            horario.Ativo = true;
            _context.Horarios.Update(horario);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
