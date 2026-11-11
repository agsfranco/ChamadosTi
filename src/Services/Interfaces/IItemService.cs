using ChamadosTi.Models;

namespace ChamadosTi.Services.Interfaces
{
    public interface IItemService : ICatalogoNomeadoService<Item>
    {
        Task<List<Item>> ListarParaSelecaoAsync(int? setorId);
    }
}
