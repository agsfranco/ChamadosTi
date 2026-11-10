using ChamadosTi.Models;

namespace ChamadosTi.Services.Interfaces
{
    public interface IItemService : ICatalogoService<Item>
    {
        Task<List<Ticket>> ObterChamadosRelacionadosAsync(int itemId);
    }
}
