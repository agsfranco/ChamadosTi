using ChamadosTi.Models;

namespace ChamadosTi.Services.Interfaces
{
    public interface ISetorService : ICatalogoService<Setor>
    {
        bool PodeAlterarFlagTi(Setor setor, bool novoValorTi);
        bool PodeSerExcluido(Setor setor);
    }
}
