using ChamadosTi.Data;
using ChamadosTi.ViewModels.Component.MenuUsuario;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChamadosTi.Components
{
    public class MenuUsuarioViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public MenuUsuarioViewComponent(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = new MenuUsuarioViewModel();
            var claimsPrincipal = UserClaimsPrincipal;

            if (claimsPrincipal.Identity != null && claimsPrincipal.Identity.IsAuthenticated)
            {
                var identityUserId = _userManager.GetUserId(claimsPrincipal);
                if (!string.IsNullOrEmpty(identityUserId))
                {
                    var usuarioLocal = await _context.Usuarios.FirstOrDefaultAsync(u => u.AspNetUserId == identityUserId);
                    model.NomeExibicao = usuarioLocal != null ? usuarioLocal.Nome : claimsPrincipal.Identity.Name ?? string.Empty;
                }
            }

            return View(model);
        }
    }
}
