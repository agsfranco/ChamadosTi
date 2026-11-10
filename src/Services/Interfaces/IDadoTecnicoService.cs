using ChamadosTi.Models;

namespace ChamadosTi.Services.Interfaces
{
    public interface IDadoTecnicoService : ICatalogoService<DadoTecnico>
    {
        Task<List<DadoTecnico>> ListarAtivosParaPerfilAsync();
    }
}
