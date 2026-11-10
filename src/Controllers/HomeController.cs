using ChamadosTi.Data;
using ChamadosTi.Models.Enums;
using ChamadosTi.ViewModels.Home;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using ChamadosTi.Services;
using ChamadosTi.Services.Interfaces;

namespace ChamadosTi.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IStringLocalizer<HomeController> _localizer;
        private readonly IUsuarioService _usuarioService;
        private readonly ITicketService _ticketService;

        public HomeController(UserManager<IdentityUser> userManager, IStringLocalizer<HomeController> localizer, IUsuarioService usuarioService, ITicketService ticketService)
        {
            _userManager = userManager;
            _localizer = localizer;
            _usuarioService = usuarioService;
            _ticketService = ticketService;
        }

        public async Task<IActionResult> Index()
        {
            var agoraUtc = DateTime.UtcNow;
            var identityUserId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(identityUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            var usuarioLocal = await _usuarioService.ObterCompletoPorAspNetIdAsync(identityUserId);

            if (usuarioLocal == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var identityUser = await _userManager.FindByIdAsync(identityUserId);
            var ehAdmin = User.IsInRole(UsuarioRole.Admin.ToString());
            var ehTecnico = User.IsInRole(UsuarioRole.Tecnico.ToString());
            var possuiAcessoGlobal = ehAdmin || ehTecnico;

            if (usuarioLocal.SetorId == null || ((ehAdmin || ehTecnico) && usuarioLocal.DadoTecnicoId == null))
            {
                return RedirectToAction("CompleteProfile", "Account");
            }

            var indicadores = await _ticketService.ContarIndicadoresAsync(possuiAcessoGlobal ? null : usuarioLocal.Id, agoraUtc);

            var modelDashboard = new IndexViewModel
            {
                TotalNovosAbertos = indicadores.TotalNovosAbertos,
                TotalEmAtendimento = indicadores.TotalEmAtendimento,
                TotalPendentes = indicadores.TotalPendentes,
                TotalSlaVencidos = indicadores.TotalSlaVencidos
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
