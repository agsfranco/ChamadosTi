using ChamadosTi.Models;

namespace ChamadosTi.Services.Interfaces
{
    public interface IItemService : ICatalogoService<Item>
    {
        Task<List<Item>> ListarParaSelecaoAsync(int? setorId);
    }
}
