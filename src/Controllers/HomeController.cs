using ChamadosTi.Data;
using ChamadosTi.Models.Enums;
using ChamadosTi.ViewModels.Home;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IStringLocalizer<HomeController> _localizer;

        public HomeController(AppDbContext context, UserManager<IdentityUser> userManager, IStringLocalizer<HomeController> localizer)
        {
            _context = context;
            _userManager = userManager;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            var agoraUtc = DateTime.UtcNow;
            var identityUserId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(identityUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            var usuarioLocal = await _context.Usuarios.FirstOrDefaultAsync(u => u.AspNetUserId == identityUserId);
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

            var queryBase = _context.Tickets.Where(t => t.Ativo == true);

            if (!possuiAcessoGlobal)
            {
                queryBase = queryBase.Where(t => t.UsuarioId == usuarioLocal.Id);
            }

            var novosAbertos = await queryBase
                .CountAsync(t => t.Status.Codigo == (int)TicketStatus.Aberto);

            var emAtendimento = await queryBase
                .CountAsync(t => t.Status.Codigo == (int)TicketStatus.EmAtendimento);

            var pendentes = await queryBase
                .CountAsync(t => t.Status.Codigo == (int)TicketStatus.Pendente);

            var slaVencidos = await queryBase
                .CountAsync(t => t.Status.Codigo < (int)TicketStatus.Resolvido 
                              && t.PrazoResolucaoSLA < agoraUtc);

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
