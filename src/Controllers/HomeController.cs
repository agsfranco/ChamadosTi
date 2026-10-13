using ChamadosTi.Data;
using ChamadosTi.Models.Enums;
using ChamadosTi.ViewModels.Home;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ChamadosTi.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var agoraUtc = DateTime.UtcNow;

            var novosAbertos = await _context.Tickets
                .CountAsync(t => t.Status.Codigo == (int)TicketStatus.Aberto && t.Ativo == true);

            var emAtendimento = await _context.Tickets
                .CountAsync(t => t.Status.Codigo == (int)TicketStatus.EmAtendimento && t.Ativo == true);

            var pendentes = await _context.Tickets
                .CountAsync(t => t.Status.Codigo == (int)TicketStatus.Pendente && t.Ativo == true);

            var slaVencidos = await _context.Tickets
                .CountAsync(t => t.Status.Codigo < (int)TicketStatus.Resolvido 
                              && t.PrazoResolucaoSLA < agoraUtc 
                              && t.Ativo == true);

            var modelDashboard = new IndexViewModel
            {
                TotalNovosAbertos = novosAbertos,
                TotalEmAtendimento = emAtendimento,
                TotalPendentes = pendentes,
                TotalSlaVencidos = slaVencidos
            };

            return View(modelDashboard);
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), Secure = true, SameSite = SameSiteMode.Lax }
            );

            if (string.IsNullOrEmpty(returnUrl))
            {
                return RedirectToAction("Index", "Home");
            }

            return LocalRedirect(returnUrl);
        }

    }
}

