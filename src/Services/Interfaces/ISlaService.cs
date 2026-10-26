using ChamadosTi.ViewModels.Sla;

namespace ChamadosTi.Services.Interfaces
{
    public interface ISlaService
    {
        Task<SlaDashboardViewModel> ObterDadosDashboardSlaAsync(DateOnly prazoInicial, DateOnly prazoFinal);
    }
}