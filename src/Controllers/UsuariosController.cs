using ChamadosTi.Models.Enums;
using ChamadosTi.Services.Interfaces;
using ChamadosTi.ViewModels.Usuario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsuariosController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IStringLocalizer<UsuariosController> _localizer;
        private readonly IUsuarioService _usuarioService;

        public UsuariosController(UserManager<IdentityUser> userManager, IStringLocalizer<UsuariosController> localizer, IUsuarioService usuarioService)
        {
            _userManager = userManager;
            _localizer = localizer;
            _usuarioService = usuarioService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(bool exibirInativos = false)
        {
            var usuarios = await _usuarioService.ListarAsync(exibirInativos);

            var listaViewModels = usuarios.Select(u => new UsuarioIndexViewModel
            {
                Id = u.Id,
                Nome = u.Nome,
                Email = u.Email,
                SetorNome = u.SetorNome,
                EhTecnico = u.EhTecnico,
                EhAdmin = u.EhAdmin,
                EhAdminMaster = u.EhAdminMaster,
                Ativo = u.Ativo
            }).ToList();

            ViewBag.ExibirInativos = exibirInativos;
            return View(listaViewModels);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoverParaTecnico(int id)
        {
            var resultado = await _usuarioService.PromoverParaTecnicoAsync(id);
            return TratarResultado(resultado, "Success_PromovidoTecnico");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoverDeTecnico(int id)
        {
            var resultado = await _usuarioService.RemoverDeTecnicoAsync(id);
            return TratarResultado(resultado, "Success_RemovidoTecnico");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoverParaAdmin(int id)
        {
            var resultado = await _usuarioService.PromoverParaAdminAsync(id);
            return TratarResultado(resultado, "Success_PromovidoAdmin");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoverDeAdmin(int id)
        {
            var aspNetUserIdLogado = _userManager.GetUserId(User);
            var resultado = await _usuarioService.RemoverDeAdminAsync(id, aspNetUserIdLogado);
            return TratarResultado(resultado, "Success_RemovidoAdmin");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Inativar(int id)
        {
            var aspNetUserIdLogado = _userManager.GetUserId(User);
            var resultado = await _usuarioService.InativarAsync(id, aspNetUserIdLogado);
            return TratarResultado(resultado, "Success_UsuarioInativado");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reativar(int id)
        {
            var resultado = await _usuarioService.ReativarAsync(id);
            return TratarResultado(resultado, "Success_UsuarioReativado");
        }

        private IActionResult TratarResultado(ResultadoOperacaoUsuario resultado, string chaveSucesso)
        {
            switch (resultado)
            {
                case ResultadoOperacaoUsuario.NaoEncontrado:
                    return NotFound();
                case ResultadoOperacaoUsuario.SetorTiNaoEncontrado:
                    TempData["ErrorMessage"] = _localizer["Error_SetorTiNaoEncontrado"].Value;
                    break;
                case ResultadoOperacaoUsuario.AutoRemocaoAdminProibida:
                    TempData["ErrorMessage"] = _localizer["Error_AutoRemocaoAdminProibida"].Value;
                    break;
                case ResultadoOperacaoUsuario.AutoInativacaoProibida:
                    TempData["ErrorMessage"] = _localizer["Error_AutoInativacaoProibida"].Value;
                    break;
                default:
                    TempData["SuccessMessage"] = _localizer[chaveSucesso].Value;
                    break;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
