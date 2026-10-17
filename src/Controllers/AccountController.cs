using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ChamadosTi.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IStringLocalizer<AccountController> _localizer;
        private readonly IEmailSender _emailSender;
        private readonly AppDbContext _context;
        private readonly IdentityOptions _identityOptions;

        public AccountController(
            SignInManager<IdentityUser> signInManager, 
            UserManager<IdentityUser> userManager,
            IStringLocalizer<AccountController> localizer,
            IEmailSender emailSender,
            AppDbContext context,
            IOptions<IdentityOptions> identityOptions)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _localizer = localizer;
            _emailSender = emailSender;
            _context = context;
            _identityOptions = identityOptions.Value;
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
                // Busca a string traduzida do recurso para e-mail não confirmado
                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    ModelState.AddModelError(string.Empty, _localizer["Error_EmailNotConfirmed"]);
                    return View(model);
                }
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var identityUser = await _userManager.FindByEmailAsync(model.Email);
                if (identityUser is not null)
                {
                    var perfilCompleto = _context.Usuarios.Any(u => u.AspNetUserId == identityUser.Id);
                
                    if (!perfilCompleto)
                    {
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
            if (!ModelState.IsValid) return View(model);

            var user = new IdentityUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                if (_identityOptions.SignIn.RequireConfirmedAccount)
                {
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.Action(
                        "ConfirmEmail", 
                        "Account", 
                        new { userId = user.Id, code = code }, 
                        protocol: Request.Scheme);

                    string assunto = _localizer["Email_ConfirmSubject"];
                    string corpo = string.Format(_localizer["Email_ConfirmBody"], callbackUrl);

                    await _emailSender.SendEmailAsync(model.Email, assunto, corpo);

                    return RedirectToAction("RegisterConfirmation", new { email = model.Email });
                } else {
                    user.EmailConfirmed = true;
                }
                await _userManager.UpdateAsync(user);
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
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

            // Busca assunto e corpo estruturado em HTML das chaves de recurso
            string assunto = _localizer["Email_ResetSubject"];
            string corpo = string.Format(_localizer["Email_ResetBody"], callbackUrl);

            await _emailSender.SendEmailAsync(model.Email, assunto, corpo);

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GenerateResetTokenForLoggedUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            return RedirectToAction("ResetPassword", new { code = code });
        }

        [HttpGet]
        public IActionResult ResetPassword(string? code = null)
        {
            if (code == null) return BadRequest(_localizer["Error_TokenRequired"]);
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

            var setorSelecionado = _context.Setores.Find(model.SetorId);

            // TRAVA REGRA DE NEGÓCIO: Se o setor selecionado for o de suporte/TI, impede o salvamento caso não possua especialidade técnica
            if (setorSelecionado != null && setorSelecionado.Ti)
            {
                if (!model.EhTecnico || model.DadoTecnicoId == null)
                {
                    ModelState.AddModelError("SetorId", _localizer["Error_SetorTiExigeDadosTecnicos"]);
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Setores = _context.Setores.OrderBy(s => s.Nome).ToList();
                ViewBag.DadosTecnicos = _context.DadosTecnicos
                    .Include(d => d.Especialidades)
                    .OrderBy(d => d.Especialidades != null ? d.Especialidades.Nome : "")
                    .ThenBy(d => d.Nivel)
                    .Select(d => new { 
                        Id = d.Id, 
                        DescricaoFormatada = (d.Especialidades != null ? d.Especialidades.Nome : "") + " - " + d.Nivel 
                    }).ToList();
                
                var total = _context.Usuarios.Count();
                var atual = _context.Usuarios.FirstOrDefault(u => u.AspNetUserId == identityUserId);
                ViewBag.PermitirMudarStatusTecnico = total == 0 || (atual != null && atual.EhTecnico);
                
                return View(model);
            }

            var usuarioExistente = _context.Usuarios.FirstOrDefault(u => u.AspNetUserId == identityUserId);
            var totalUsuariosSistema = _context.Usuarios.Count();

            if (!model.EhTecnico)
            {
                model.DadoTecnicoId = null;
            }

            if (usuarioExistente == null)
            {
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