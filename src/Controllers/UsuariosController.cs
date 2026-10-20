using ChamadosTi.Data;
using ChamadosTi.Models;
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

        public async Task<IActionResult> Index(bool exibirInativos = false)
        {
            var query = _context.Usuarios.Include(u => u.Setores).AsQueryable();

            if (exibirInativos)
            {
                query = query.IgnoreQueryFilters().Where(u => !u.Ativo);
            }
            else
            {
                query = query.Where(u => u.Ativo);
            }

            var usuariosBase = await query.OrderBy(u => u.Nome).ToListAsync();
            var listaViewModels = new List<UsuarioIndexViewModel>();

            foreach (var u in usuariosBase)
            {
                var identityUser = await _userManager.FindByIdAsync(u.AspNetUserId);
                var email = identityUser?.Email ?? string.Empty;
                var ehTecnico = identityUser != null && await _userManager.IsInRoleAsync(identityUser, UsuarioRole.Tecnico.ToString());
                var ehAdmin = identityUser != null && await _userManager.IsInRoleAsync(identityUser, UsuarioRole.Admin.ToString());
                var ehAdminMaster = identityUser != null && await _userManager.IsInRoleAsync(identityUser, UsuarioRole.AdminMaster.ToString());

                listaViewModels.Add(new UsuarioIndexViewModel
                {
                    Id = u.Id,
                    Nome = u.Nome,
                    Email = email,
                    SetorNome = u.Setores != null ? u.Setores.Nome : string.Empty,
                    EhTecnico = ehTecnico,
                    EhAdmin = ehAdmin,
                    EhAdminMaster = ehAdminMaster,
                    Ativo = u.Ativo
                });
            }

            ViewBag.ExibirInativos = exibirInativos;
            return View(listaViewModels);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoverParaTecnico(int id)
        {
            var usuario = await GetUsuario(id);
            if (usuario == null) return NotFound();

            var identityUser = await _userManager.FindByIdAsync(usuario.AspNetUserId);
            if (identityUser == null) return NotFound();

            var setorTiOficial = await GetSetorTi();
            if (setorTiOficial == null)
            {
                TempData["ErrorMessage"] = _localizer["Error_SetorTiNaoEncontrado"].Value;
                return RedirectToAction(nameof(Index));
            }

            usuario.SetorId = setorTiOficial.Id;
            _context.Usuarios.Update(usuario);

            await _userManager.AddToRoleAsync(identityUser, UsuarioRole.Tecnico.ToString());
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Success_PromovidoTecnico"].Value;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoverDeTecnico(int id)
        {
            var usuario = await GetUsuario(id);
            if (usuario == null) return NotFound();

            var identityUser = await _userManager.FindByIdAsync(usuario.AspNetUserId);
            if (identityUser == null) return NotFound();

            usuario.DadoTecnicoId = null;
            usuario.SetorId = null;
            
            _context.Usuarios.Update(usuario);

            await _userManager.RemoveFromRoleAsync(identityUser, UsuarioRole.Tecnico.ToString());
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Success_RemovidoTecnico"].Value;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoverParaAdmin(int id)
        {
            var identityUser = await ObterIdentityUserPorIdLocalAsync(id);
            if (identityUser == null) return NotFound();            
            
            var usuario = await GetUsuario(id);
            if (usuario == null) return NotFound(); 

            var setorTi = await GetSetorTi();
            if (setorTi == null)
            {
                TempData["ErrorMessage"] = _localizer["Error_SetorTiNaoEncontrado"].Value;
                return RedirectToAction(nameof(Index));
            }

            usuario.SetorId = setorTi.Id;

            await _userManager.AddToRoleAsync(identityUser, UsuarioRole.Tecnico.ToString());
            await _userManager.AddToRoleAsync(identityUser, UsuarioRole.Admin.ToString());            

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Success_PromovidoAdmin"].Value;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoverDeAdmin(int id)
        {
            var usuario = await GetUsuario(id);
            if (usuario == null) return NotFound();

            var aspNetUserId = _userManager.GetUserId(User);
            if (usuario.AspNetUserId == aspNetUserId)
            {
                TempData["ErrorMessage"] = _localizer["Error_AutoRemocaoAdminProibida"].Value;
                return RedirectToAction(nameof(Index));
            }

            var identityUser = await _userManager.FindByIdAsync(usuario.AspNetUserId);
            if (identityUser == null) return NotFound();

            await _userManager.RemoveFromRoleAsync(identityUser, UsuarioRole.Admin.ToString());
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Success_RemovidoAdmin"].Value;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Inativar(int id)
        {
            var usuario = await GetUsuario(id);
            if (usuario == null) return NotFound();

            var logadoId = _userManager.GetUserId(User);
            if (usuario.AspNetUserId == logadoId)
            {
                TempData["ErrorMessage"] = _localizer["Error_AutoInativacaoProibida"].Value;
                return RedirectToAction(nameof(Index));
            }

            usuario.Ativo = false;

            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Success_UsuarioInativado"].Value;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reativar(int id)
        {
            var usuario = await GetUsuario(id);
            if (usuario == null) return NotFound();

            usuario.Ativo = true;
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["Success_UsuarioReativado"].Value;
            return RedirectToAction(nameof(Index));
        }

        private async Task<IdentityUser?> ObterIdentityUserPorIdLocalAsync(int id)
        {
            var usuario = await GetUsuario(id);
            if (usuario == null) return null;
            return await _userManager.FindByIdAsync(usuario.AspNetUserId);
        }

        private async Task<Usuario?> GetUsuario(int id)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
        }

        private async Task<Setor?> GetSetorTi()
        {
            return await _context.Setores.FirstOrDefaultAsync(s => s.Ti);
        }
    }
}
