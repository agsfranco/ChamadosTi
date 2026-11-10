using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChamadosTi.Services
{
    public class CategoriaService : CatalogoService<Categoria>, ICategoriaService
    {
        public CategoriaService(AppDbContext context) : base(context)
        {
        }

        protected override IQueryable<Categoria> IncluirRelacionamentos(IQueryable<Categoria> query)
        {
            return query.Include(c => c.Especialidades);
        }
    }
}
