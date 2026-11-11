using ChamadosTi.Models;

namespace ChamadosTi.Services.Interfaces
{
    public interface IPrioridadeService : ICatalogoNomeadoService<Prioridade>
    {
        Task<bool> PadraoDuplicadoAsync(int id, bool ehPadrao);
        Task<Prioridade?> ObterPadraoAsync();
        Task<List<Prioridade>> ListarOrdenadasPorIdAsync();
    }
}
