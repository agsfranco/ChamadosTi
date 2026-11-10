using ChamadosTi.Models;

namespace ChamadosTi.Services.Interfaces
{
    public interface IHorarioService : ICatalogoService<Horario>
    {
        bool JornadaValida(TimeOnly? jornadaInicio, TimeOnly? jornadaFim, TimeOnly? almocoInicio, TimeOnly? almocoFim);
        Task CriarComSetoresAsync(Horario horario, IEnumerable<int>? setoresIds);
        Task AtualizarComSetoresAsync(Horario horario, IEnumerable<int>? setoresIds);
        Task RemoverComSetoresAsync(Horario horario);
    }
}
