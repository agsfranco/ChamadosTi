using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChamadosTi.Services
{
    public class ItemService : CatalogoService<Item>, IItemService
    {
        public ItemService(AppDbContext context) : base(context)
        {
        }

        protected override IQueryable<Item> IncluirRelacionamentos(IQueryable<Item> query)
        {
            return query.Include(i => i.Tipos)
                        .Include(i => i.Marcas)
                        .Include(i => i.Setores);
        }

        public async Task<List<Ticket>> ObterChamadosRelacionadosAsync(int itemId)
        {
            return await _context.Tickets
                .Include(t => t.Status)
                .Include(t => t.AuditoriaTickets)
                .Where(t => t.ItensTickets!.Any(it => it.ItemId == itemId))
                .OrderByDescending(t => t.DataAbertura)
                .ToListAsync();
        }
    }
}
