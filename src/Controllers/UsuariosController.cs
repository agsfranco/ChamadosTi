using ChamadosTi.Data;
using ChamadosTi.Models.Enums;
using ChamadosTi.ViewModels.Usuario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsuariosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IStringLocalizer<UsuariosController> _localizer;

        public UsuariosController(AppDbContext context, UserManager<IdentityUser> userManager, IStringLocalizer<UsuariosController> localizer)
        {
            _context = context;
            _userManager = userManager;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            var usuariosBase = await _context.Usuarios
                .Include(u => u.Setores)
                .Where(u => u.Ativo)
                .OrderBy(u => u.Nome)
                .ToListAsync();

            var listaViewModels = new List<UsuarioIndexViewModel>();

            foreach (var u in usuariosBase)
            {
                var identityUser = await _userManager.FindByIdAsync(u.AspNetUserId);
                var email = identityUser?.Email ?? string.Empty;
                var ehTecnico = identityUser != null && await _userManager.IsInRoleAsync(identityUser, UsuarioRole.Tecnico.ToString());

                listaViewModels.Add(new UsuarioIndexViewModel
                {
                    Id = u.Id,
                    Nome = u.Nome,
                    Email = email,
                    SetorNome = u.Setores != null ? u.Setores.Nome : string.Empty,
                    EhTecnico = ehTecnico
                });
            }

            return View(listaViewModels);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoverParaTecnico(int id)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id && u.Ativo);
            if (usuario == null)
            {
                TempData["ErrorMessage"] = _localizer["Error_UsuarioNaoEncontrado"].Value;
                return RedirectToAction(nameof(Index));
            }

            var identityUser = await _userManager.FindByIdAsync(usuario.AspNetUserId);
            if (identityUser == null)
            {
                TempData["ErrorMessage"] = _localizer["Error_UsuarioNaoEncontrado"].Value;
                return RedirectToAction(nameof(Index));
            }

            var jaEhTecnico = await _userManager.IsInRoleAsync(identityUser, UsuarioRole.Tecnico.ToString());
            if (jaEhTecnico)
            {
                TempData["ErrorMessage"] = _localizer["Error_JaEhTecnico"].Value;
                return RedirectToAction(nameof(Index));
            }

            await _userManager.AddToRoleAsync(identityUser, UsuarioRole.Tecnico.ToString());

            TempData["SuccessMessage"] = _localizer["Success_PromovidoTecnico"].Value;
            return RedirectToAction(nameof(Index));
        }
    }
}
