using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.Services.Interfaces;

namespace ChamadosTi.Services
{
    public class EspecialidadeService : CatalogoService<Especialidade>, IEspecialidadeService
    {
        public EspecialidadeService(AppDbContext context) : base(context)
        {
        }

        // A listagem de especialidades não é ordenada (comportamento original mantido).
        protected override IQueryable<Especialidade> OrdenarListagem(IQueryable<Especialidade> query)
        {
            return query;
        }
    }
}
