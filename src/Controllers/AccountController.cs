using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
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

        // GET: Account/CompleteProfile
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> CompleteProfile()
        {
            var identityUserId = _userManager.GetUserId(User);
            ViewBag.Setores = _context.Setores.ToList();

            // Busca se o usuário já existe na tabela complementar
            var usuarioExistente = _context.Usuarios.FirstOrDefault(u => u.AspNetUserId == identityUserId);

            // Se NÃO existir, abre a tela com os campos vazios (Criação)
            if (usuarioExistente == null)
            {
                return View(new CompleteProfileViewModel());
            }

            // Se JÁ existir, popula a ViewModel com os dados do banco (Edição)
            var model = new CompleteProfileViewModel
            {
                Nome = usuarioExistente.Nome,
                SetorId = usuarioExistente.SetorId
            };

            return View(model);
        }


        // POST: Account/CompleteProfile
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteProfile(CompleteProfileViewModel model)
        {
            var identityUserId = _userManager.GetUserId(User);

            if (!ModelState.IsValid)
            {
                ViewBag.Setores = _context.Setores.ToList();
                return View(model);
            }

            // Busca se o registro já existe no banco
            var usuarioExistente = _context.Usuarios.FirstOrDefault(u => u.AspNetUserId == identityUserId);

            if (usuarioExistente == null)
            {
                // MODO CRIAÇÃO: Cria um novo registro se for o primeiro login
                var novoUsuario = new Usuario
                {
                    AspNetUserId = identityUserId!,
                    Nome = model.Nome,
                    SetorId = model.SetorId!.Value, // Usamos !.Value pois agora é int? na ViewModel
                    EhTecnico = false,
                    Ativo = true
                };

                _context.Usuarios.Add(novoUsuario);
            }
            else
            {
                // MODO EDIÇÃO: Atualiza os dados existentes
                usuarioExistente.Nome = model.Nome;
                usuarioExistente.SetorId = model.SetorId!.Value;

                _context.Usuarios.Update(usuarioExistente);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
