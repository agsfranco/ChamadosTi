using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChamadosTi.Services
{
    public class ItemService : CatalogoNomeadoService<Item>, IItemService
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

        // Itens para seleção em chamados, filtrados pelo setor quando informado.
        public async Task<List<Item>> ListarParaSelecaoAsync(int? setorId)
        {
            var query = _context.Itens
                                .Include(i => i.Tipos)
                                .Include(i => i.Marcas)
                                .Include(i => i.Setores)
                                .AsQueryable();

            if (setorId != null)
            {
                query = query.Where(i => i.SetorId == setorId);
            }

            return await query.OrderBy(i => i.Nome)
                              .ThenBy(i => i.Tipos.Nome)
                              .ThenBy(i => i.Marcas.Nome)
                              .ToListAsync();
        }
    }
}
