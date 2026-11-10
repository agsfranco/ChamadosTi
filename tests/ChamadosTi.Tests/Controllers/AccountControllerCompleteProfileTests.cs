using System.Security.Claims;
using ChamadosTi.Controllers;
using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.Services;
using ChamadosTi.Services.Interfaces;
using ChamadosTi.ViewModels.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ChamadosTi.Tests.Controllers
{
    public class AccountControllerCompleteProfileTests : IDisposable
    {
        private readonly SqliteConnection _conexao;
        private readonly AppDbContext _ctx;
        private readonly Mock<UserManager<IdentityUser>> _userManager;
        private readonly AccountController _controller;

        public AccountControllerCompleteProfileTests()
        {
            _conexao = new SqliteConnection("DataSource=:memory:");
            _conexao.Open();
            _ctx = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conexao).Options);
            _ctx.Database.EnsureCreated();

            _userManager = new Mock<UserManager<IdentityUser>>(
                Mock.Of<IUserStore<IdentityUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
            _userManager
                .Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns<ClaimsPrincipal>(p => p.FindFirstValue(ClaimTypes.NameIdentifier));
            _userManager
                .Setup(u => u.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => new IdentityUser { Id = id, UserName = "a@teste.com" });

            var signInManager = new Mock<SignInManager<IdentityUser>>(
                _userManager.Object, Mock.Of<IHttpContextAccessor>(), Mock.Of<IUserClaimsPrincipalFactory<IdentityUser>>(),
                null!, null!, null!, null!);

            var localizer = new Mock<IStringLocalizer<AccountController>>();
            localizer.Setup(l => l[It.IsAny<string>()]).Returns<string>(chave => new LocalizedString(chave, chave));

            _controller = new AccountController(
                signInManager.Object,
                _userManager.Object,
                localizer.Object,
                Mock.Of<IEmailSender>(),
                Mock.Of<IUsuarioService>(),
                new SetorService(_ctx),
                new DadoTecnicoService(_ctx),
                Options.Create(new IdentityOptions()));

            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "aspnet-1") }, "teste"));
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } };
        }

        [Fact]
        public async Task CompleteProfilePost_SetorInexistente_RetornaViewComErroSemExcecao()
        {
            var model = new CompleteProfileViewModel { Nome = "Ana", SetorId = 999, EhTecnico = false };

            var resultado = await _controller.CompleteProfile(model);

            var view = Assert.IsType<ViewResult>(resultado);
            Assert.Same(model, view.Model);
            Assert.True(_controller.ModelState.ContainsKey("SetorId"));
            Assert.False(_controller.ModelState.IsValid);
        }

        public void Dispose()
        {
            _ctx.Dispose();
            _conexao.Dispose();
        }
    }
}
