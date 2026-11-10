using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChamadosTi.Services
{
    public class HorarioService : CatalogoService<Horario>, IHorarioService
    {
        public HorarioService(AppDbContext context) : base(context)
        {
        }

        protected override IQueryable<Horario> OrdenarListagem(IQueryable<Horario> query)
        {
            return query.OrderBy(h => h.DiaDaSemana);
        }

        protected override IQueryable<Horario> IncluirRelacionamentos(IQueryable<Horario> query)
        {
            return query.Include(h => h.SetoresHorarios!)
                        .ThenInclude(sh => sh.Setor);
        }

        // A jornada deve terminar depois de começar, e o almoço também.
        public bool JornadaValida(TimeOnly? jornadaInicio, TimeOnly? jornadaFim, TimeOnly? almocoInicio, TimeOnly? almocoFim)
        {
            return !(jornadaFim <= jornadaInicio || almocoFim <= almocoInicio);
        }

        public async Task CriarComSetoresAsync(Horario horario, IEnumerable<int>? setoresIds)
        {
            await CriarAsync(horario);

            if (setoresIds != null && setoresIds.Any())
            {
                foreach (var setorId in setoresIds)
                {
                    _context.SetoresHorarios.Add(new SetorHorario
                    {
                        HorarioId = horario.Id,
                        SetorId = setorId,
                        Ativo = true
                    });
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task AtualizarComSetoresAsync(Horario horario, IEnumerable<int>? setoresIds)
        {
            if (horario.SetoresHorarios != null)
            {
                _context.SetoresHorarios.RemoveRange(horario.SetoresHorarios);
            }

            if (setoresIds != null && setoresIds.Any())
            {
                foreach (var setorId in setoresIds)
                {
                    _context.SetoresHorarios.Add(new SetorHorario
                    {
                        HorarioId = horario.Id,
                        SetorId = setorId,
                        Ativo = true
                    });
                }
            }

            await AtualizarAsync(horario);
        }

        public async Task RemoverComSetoresAsync(Horario horario)
        {
            if (horario.SetoresHorarios != null)
            {
                _context.SetoresHorarios.RemoveRange(horario.SetoresHorarios);
            }

            await RemoverAsync(horario);
        }
    }
}
