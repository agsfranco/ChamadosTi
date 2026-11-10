using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChamadosTi.Services
{
    public class FeriadoService : CatalogoService<Feriado>, IFeriadoService
    {
        public FeriadoService(AppDbContext context) : base(context)
        {
        }

        protected override IQueryable<Feriado> OrdenarListagem(IQueryable<Feriado> query)
        {
            return query.OrderBy(f => f.Dia);
        }

        // Não pode haver dois feriados no mesmo dia; ignora o próprio registro na edição.
        public async Task<bool> DiaDuplicadoAsync(DateOnly dia, int id)
        {
            return await _context.Feriados.AnyAsync(f => f.Dia == dia && f.Id != id);
        }
    }
}
