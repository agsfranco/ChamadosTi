using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChamadosTi.Services
{
    public class PrioridadeService : CatalogoService<Prioridade>, IPrioridadeService
    {
        public PrioridadeService(AppDbContext context) : base(context)
        {
        }

        // Só pode existir uma prioridade padrão ativa; retorna true se já existe outra.
        public async Task<bool> PadraoDuplicadoAsync(int id, bool ehPadrao)
        {
            if (!ehPadrao) return false;
            return await _context.Prioridades.AnyAsync(x => x.Id != id && x.Padrao && x.Ativo);
        }
    }
}
