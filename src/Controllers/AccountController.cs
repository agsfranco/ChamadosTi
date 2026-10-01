using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IStringLocalizer<AccountController> _localizer;
        private readonly IEmailSender _emailSender;

        private readonly AppDbContext _context;

        public AccountController(
            SignInManager<IdentityUser> signInManager, 
            UserManager<IdentityUser> userManager,
            IStringLocalizer<AccountController> localizer,
            IEmailSender emailSender,
            AppDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _localizer = localizer;
            _emailSender = emailSender;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                // Verifica se o e-mail foi confirmado antes de tentar o login para evitar exceções de SignIn
                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    ModelState.AddModelError(string.Empty, _localizer["Error_EmailNotConfirmed"] ?? "Confirme seu e-mail primeiro.");
                    return View(model);
                }
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // VERIFICAÇÃO DO COMPLEMENTO DE CADASTRO
                var identityUser = await _userManager.FindByEmailAsync(model.Email);
                if (identityUser is not null)
                {
                    var perfilCompleto = _context.Usuarios.Any(u => u.AspNetUserId == identityUser.Id);
                
                    if (!perfilCompleto)
                    {
                        // Se não completou o perfil, manda para a tela de finalização
                        return RedirectToAction("CompleteProfile");
                    }
                }

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, _localizer["Error_LockedOut"]);
                return View(model);
            }

            ModelState.AddModelError(string.Empty, _localizer["Error_InvalidLogin"]);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new IdentityUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = Url.Action(
                    "ConfirmEmail", 
                    "Account", 
                    new { userId = user.Id, code = code }, 
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(model.Email, "Confirme sua conta", 
                    $"Por favor, confirme sua conta <a href='{callbackUrl}'>clicando aqui</a>.");

                return RedirectToAction("RegisterConfirmation", new { email = model.Email });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult RegisterConfirmation(string email)
        {
            if (email == null) return RedirectToAction("Index", "Home");
            ViewBag.Email = email;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null) return RedirectToAction("Index", "Home");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound(_localizer["Error_UserNotFound"]);

            var result = await _userManager.ConfirmEmailAsync(user, code);
            
            // Substituído por chaves do localizer
            ViewBag.StatusMessage = result.Succeeded 
                ? _localizer["Success_EmailConfirmed"] 
                : _localizer["Error_EmailConfirmationFailed"];
                
            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(model.Email, "Redefinição de Senha",
                $"Redefina sua senha <a href='{callbackUrl}'>clicando aqui</a>.");

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // GET: Account/GenerateResetTokenForLoggedUser
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GenerateResetTokenForLoggedUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            // Gera o token de redefinição de forma legítima pois o usuário está autenticado
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Redireciona para o ResetPassword passando o token de segurança obrigatório
            return RedirectToAction("ResetPassword", new { code = code });
        }

        [HttpGet]
        public IActionResult ResetPassword(string? code = null)
        {
            if (code == null) return BadRequest("Um token válido deve ser fornecido.");
            return View(new ResetPasswordViewModel { Code = code });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return RedirectToAction("ResetPasswordConfirmation");

            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded) return RedirectToAction("ResetPasswordConfirmation");

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> CompleteProfile()
        {
            var identityUserId = _userManager.GetUserId(User);
            
            ViewBag.Setores = _context.Setores.OrderBy(s => s.Nome).ToList();
            
            ViewBag.DadosTecnicos = _context.DadosTecnicos
            .Include(d => d.Especialidades)
            .OrderBy(d => d.Especialidades != null ? d.Especialidades.Nome : "")
            .ThenBy(d => d.Nivel)
            .Select(d => new {
                Id = d.Id,
                DescricaoFormatada = (d.Especialidades != null ? d.Especialidades.Nome : "") + " - " + d.Nivel
            }).ToList();

            // Verifica se o usuário pode gerenciar o próprio status de técnico
            // Só permitimos se ele for o primeiro usuário do sistema ou se ele mesmo já for um técnico editando seu perfil
            var totalUsuarios = _context.Usuarios.Count();
            var usuarioExistente = _context.Usuarios.FirstOrDefault(u => u.AspNetUserId == identityUserId);
            
            ViewBag.PermitirMudarStatusTecnico = totalUsuarios == 1 || (usuarioExistente != null && usuarioExistente.EhTecnico);

            if (usuarioExistente == null)
            {
                return View(new CompleteProfileViewModel());
            }

            var model = new CompleteProfileViewModel
            {
                Nome = usuarioExistente.Nome,
                SetorId = usuarioExistente.SetorId,
                EhTecnico = usuarioExistente.EhTecnico,
                DadoTecnicoId = usuarioExistente.DadoTecnicoId
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteProfile(CompleteProfileViewModel model)
        {
            var identityUserId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(identityUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Setores = _context.Setores.ToList();
                ViewBag.DadosTecnicos = _context.DadosTecnicos.Include(d => d.Especialidades)
                    .Select(d => new { Id = d.Id, DescricaoFormatada = d.Nivel + " - " + (d.Especialidades != null ? d.Especialidades.Nome : "") }).ToList();
                
                var total = _context.Usuarios.Count();
                var atual = _context.Usuarios.FirstOrDefault(u => u.AspNetUserId == identityUserId);
                ViewBag.PermitirMudarStatusTecnico = total == 0 || (atual != null && atual.EhTecnico);
                
                return View(model);
            }

            var usuarioExistente = _context.Usuarios.FirstOrDefault(u => u.AspNetUserId == identityUserId);
            var totalUsuariosSistema = _context.Usuarios.Count();

            // Se o usuário desmarcou o checkbox ou não preencheu, limpa a FK
            if (!model.EhTecnico)
            {
                model.DadoTecnicoId = null;
            }

            if (usuarioExistente == null)
            {
                // Se for o primeiro usuário absoluto, ele pode se auto-promover, senão força false
                bool definirComoTecnico = totalUsuariosSistema == 0 ? model.EhTecnico : false;

                var novoUsuario = new Usuario
                {
                    AspNetUserId = identityUserId,
                    Nome = model.Nome,
                    SetorId = model.SetorId!.Value,
                    EhTecnico = definirComoTecnico,
                    DadoTecnicoId = definirComoTecnico ? model.DadoTecnicoId : null,
                    Ativo = true
                };

                _context.Usuarios.Add(novoUsuario);
            }
            else
            {
                usuarioExistente.Nome = model.Nome;
                usuarioExistente.SetorId = model.SetorId!.Value;
                
                // Só aplica a alteração do checkbox de técnico se a ViewBag deu permissão legítima
                if (totalUsuariosSistema == 1 || usuarioExistente.EhTecnico)
                {
                    usuarioExistente.EhTecnico = model.EhTecnico;
                    usuarioExistente.DadoTecnicoId = model.DadoTecnicoId;
                }

                _context.Usuarios.Update(usuarioExistente);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
