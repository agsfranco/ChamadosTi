using ChamadosTi.Models;

namespace ChamadosTi.Services.Interfaces
{
    public interface IPrioridadeService : ICatalogoService<Prioridade>
    {
        Task<bool> PadraoDuplicadoAsync(int id, bool ehPadrao);
        Task<Prioridade?> ObterPadraoAsync();
        Task<List<Prioridade>> ListarOrdenadasPorIdAsync();
    }
}
