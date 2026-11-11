using ChamadosTi.Data;
using ChamadosTi.Models.Interfaces;
using ChamadosTi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChamadosTi.Services
{
    public abstract class CatalogoNomeadoService<T> : CatalogoService<T>, ICatalogoNomeadoService<T> where T : class, ISoftDelete, INomeado
    {
        protected CatalogoNomeadoService(AppDbContext context) : base(context)
        {
        }

        // Ordenação padrão da listagem: por Nome. Cadastros que não ordenam sobrescrevem.
        protected override IQueryable<T> OrdenarListagem(IQueryable<T> query)
        {
            return query.OrderBy(e => e.Nome);
        }

        public async Task<List<T>> ListarOrdenadosPorNomeAsync()
        {
            return await Conjunto.OrderBy(e => e.Nome).ToListAsync();
        }
    }
}
