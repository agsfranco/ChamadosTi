using ChamadosTi.Models;

namespace ChamadosTi.Services.Interfaces
{
    public interface IFeriadoService : ICatalogoService<Feriado>
    {
        Task<bool> DiaDuplicadoAsync(DateOnly dia, int id);
    }
}
