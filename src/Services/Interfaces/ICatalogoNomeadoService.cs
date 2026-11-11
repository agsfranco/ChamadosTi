using ChamadosTi.Models.Interfaces;

namespace ChamadosTi.Services.Interfaces
{
    public interface ICatalogoNomeadoService<T> : ICatalogoService<T> where T : class, ISoftDelete, INomeado
    {
        Task<List<T>> ListarOrdenadosPorNomeAsync();
    }
}
