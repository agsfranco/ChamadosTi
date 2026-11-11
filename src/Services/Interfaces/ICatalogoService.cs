using ChamadosTi.Models.Interfaces;

namespace ChamadosTi.Services.Interfaces
{
    public interface ICatalogoService<T> where T : class, ISoftDelete
    {
        Task<List<T>> ListarAsync(bool exibirInativos);
        Task<T?> ObterPorIdAsync(int id, bool ignorarFiltros = false, bool incluirRelacionamentos = false);
        Task CriarAsync(T entidade);
        Task AtualizarAsync(T entidade);
        Task RemoverAsync(T entidade);
        Task AtivarAsync(T entidade);
    }
}
