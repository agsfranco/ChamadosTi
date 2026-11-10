using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChamadosTi.Services
{
    public class SetorService : CatalogoService<Setor>, ISetorService
    {
        public SetorService(AppDbContext context) : base(context)
        {
        }

        // A listagem de setores não é ordenada (comportamento original mantido).
        protected override IQueryable<Setor> OrdenarListagem(IQueryable<Setor> query)
        {
            return query;
        }

        // O setor de TI não pode deixar de ser TI.
        public bool PodeAlterarFlagTi(Setor setor, bool novoValorTi)
        {
            return !(setor.Ti && !novoValorTi);
        }

        // O setor de TI não pode ser excluído.
        public bool PodeSerExcluido(Setor setor)
        {
            return !setor.Ti;
        }

        // Apenas um setor pode ser TI: ao marcar um como TI, os demais são desmarcados.
        public override async Task CriarAsync(Setor setor)
        {
            if (setor.Ti)
            {
                await DesmarcarOutrosSetoresTiAsync(0);
            }

            await base.CriarAsync(setor);
        }

        public override async Task AtualizarAsync(Setor setor)
        {
            if (setor.Ti)
            {
                await DesmarcarOutrosSetoresTiAsync(setor.Id);
            }

            await base.AtualizarAsync(setor);
        }

        private async Task DesmarcarOutrosSetoresTiAsync(int setorAtualId)
        {
            await _context.Setores
                .Where(s => s.Ti && s.Id != setorAtualId)
                .ExecuteUpdateAsync(set => set.SetProperty(s => s.Ti, false));
        }
    }
}
