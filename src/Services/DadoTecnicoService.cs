using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChamadosTi.Services
{
    public class DadoTecnicoService : CatalogoService<DadoTecnico>, IDadoTecnicoService
    {
        public DadoTecnicoService(AppDbContext context) : base(context)
        {
        }

        // A listagem de dados técnicos não é ordenada (comportamento original mantido).
        protected override IQueryable<DadoTecnico> OrdenarListagem(IQueryable<DadoTecnico> query)
        {
            return query;
        }

        protected override IQueryable<DadoTecnico> IncluirRelacionamentos(IQueryable<DadoTecnico> query)
        {
            return query.Include(d => d.Especialidades);
        }

        // Dados técnicos ativos para o formulário de perfil, ordenados pelo nome da especialidade.
        public async Task<List<DadoTecnico>> ListarAtivosParaPerfilAsync()
        {
            return await _context.DadosTecnicos
                                 .Include(d => d.Especialidades)
                                 .Where(d => d.Ativo)
                                 .OrderBy(d => d.Especialidades != null ? d.Especialidades.Nome : "")
                                 .ToListAsync();
        }
    }
}
