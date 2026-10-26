using ChamadosTi.Services;
using ChamadosTi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChamadosTi.Controllers
{
    [Authorize(Roles = "Tecnico,Admin,AdminMaster")]
    public class SlaController : Controller
    {
        private readonly ISlaService _slaService;

        public SlaController(ISlaService slaService)
        {
            _slaService = slaService;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard(DateOnly? dataInicio, DateOnly? dataFim)
        {
            // Se o usuário não filtrou, assume por padrão a janela dos últimos 30 dias
            var fim = dataFim ?? DateOnly.FromDateTime(DateTime.Today);
            var inicio = dataInicio ?? fim.AddDays(-30);

            // Passamos as datas para a ViewBag para manter os campos do formulário preenchidos na tela
            ViewBag.DataInicioFiltro = inicio.ToString("yyyy-MM-dd");
            ViewBag.DataFimFiltro = fim.ToString("yyyy-MM-dd");

            var dadosDashboard = await _slaService.ObterDadosDashboardSlaAsync(inicio, fim);
            return View(dadosDashboard);
        }

    }
}
