using ChamadosTi.Data;
using ChamadosTi.Models.Interfaces;
using ChamadosTi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChamadosTi.Services
{
    public abstract class CatalogoService<T> : ICatalogoService<T> where T : class, ISoftDelete
    {
        protected readonly AppDbContext _context;

        protected CatalogoService(AppDbContext context)
        {
            _context = context;
        }

        protected DbSet<T> Conjunto => _context.Set<T>();

        // Ordenação padrão da listagem: por Nome. Cadastros que não ordenam sobrescrevem.
        protected virtual IQueryable<T> OrdenarListagem(IQueryable<T> query)
        {
            return query.OrderBy(e => EF.Property<string>(e, "Nome"));
        }

        // Relacionamentos carregados na listagem e, quando solicitado, na busca por id.
        protected virtual IQueryable<T> IncluirRelacionamentos(IQueryable<T> query)
        {
            return query;
        }

        public virtual async Task<List<T>> ListarAsync(bool exibirInativos)
        {
            var query = exibirInativos
                ? Conjunto.IgnoreQueryFilters().Where(e => !e.Ativo)
                : Conjunto.AsQueryable();

            query = OrdenarListagem(IncluirRelacionamentos(query));
            return await query.ToListAsync();
        }

        public virtual async Task<List<T>> ListarOrdenadosPorNomeAsync()
        {
            return await Conjunto.OrderBy(e => EF.Property<string>(e, "Nome")).ToListAsync();
        }

        public virtual async Task<T?> ObterPorIdAsync(int id, bool ignorarFiltros = false, bool incluirRelacionamentos = false)
        {
            var query = ignorarFiltros
                ? Conjunto.IgnoreQueryFilters()
                : Conjunto.AsQueryable();

            if (incluirRelacionamentos)
            {
                query = IncluirRelacionamentos(query);
            }

            return await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
        }

        public virtual async Task CriarAsync(T entidade)
        {
            Conjunto.Add(entidade);
            await _context.SaveChangesAsync();
        }

        public virtual async Task AtualizarAsync(T entidade)
        {
            Conjunto.Update(entidade);
            await _context.SaveChangesAsync();
        }

        public virtual async Task RemoverAsync(T entidade)
        {
            Conjunto.Remove(entidade);
            await _context.SaveChangesAsync();
        }

        public virtual async Task AtivarAsync(T entidade)
        {
            entidade.Ativo = true;
            Conjunto.Update(entidade);
            await _context.SaveChangesAsync();
        }
    }
}
