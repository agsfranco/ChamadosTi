using System.Security.Claims;
using ChamadosTi.Controllers;
using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace ChamadosTi.Tests.Helpers
{
    /// <summary>
    /// Cria um banco SQLite em memória isolado para cada teste (o xUnit instancia a classe de teste uma vez por teste)
    /// e monta a TicketsController com as dependências mockadas.
    /// </summary>
    public sealed class TicketsControllerFixture : IDisposable
    {
        /// <summary>Quinta-feira, 29/10/2026 12:00 UTC (expediente do setor TI: 08h às 18h, almoço 12h às 13h local).</summary>
        public static readonly DateTimeOffset InstanteInicial = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

        private readonly SqliteConnection _conexao;

        public AppDbContext Contexto { get; }
        public FakeTimeProvider Relogio { get; }
        public DadosDeTeste Dados { get; }
        public Mock<IUsuarioService> UsuarioService { get; } = new();
        public Mock<UserManager<IdentityUser>> UserManager { get; }
        public Mock<IStringLocalizer<TicketsController>> Localizer { get; } = new();

        public TicketsControllerFixture()
        {
            _conexao = new SqliteConnection("DataSource=:memory:");
            _conexao.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_conexao)
                .Options;

            Contexto = new AppDbContext(options);
            Contexto.Database.EnsureCreated();

            Relogio = new FakeTimeProvider(InstanteInicial);
            Dados = SeedData.Popular(Contexto);

            // GetUserId é lido do claim NameIdentifier, igual ao comportamento real do UserManager.
            UserManager = new Mock<UserManager<IdentityUser>>(
                Mock.Of<IUserStore<IdentityUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
            UserManager
                .Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns<ClaimsPrincipal>(p => p.FindFirstValue(ClaimTypes.NameIdentifier));

            // Localizer devolve a própria chave, assim os testes verificam as chaves de mensagem.
            Localizer
                .Setup(l => l[It.IsAny<string>()])
                .Returns<string>(chave => new LocalizedString(chave, chave));

            UsuarioService
                .Setup(s => s.GetUsuarioLocalPorAspNetIdAsync(It.IsAny<string>()))
                .Returns<string>(aspNetId => Contexto.Usuarios.FirstOrDefaultAsync(u => u.AspNetUserId == aspNetId));
        }

        /// <summary>
        /// Cria a controller já autenticada como <paramref name="usuarioLogado"/>.
        /// <paramref name="ehTecnico"/> define o retorno de IUsuarioService.EhTecnico.
        /// </summary>
        public TicketsController CriarController(Usuario usuarioLogado, bool ehTecnico)
        {
            UsuarioService
                .Setup(s => s.EhTecnico(It.IsAny<ClaimsPrincipal>()))
                .Returns(ehTecnico);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuarioLogado.AspNetUserId),
                new(ClaimTypes.Name, usuarioLogado.Nome)
            };
            if (ehTecnico)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Tecnico"));
            }

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "teste"));
            var httpContext = new DefaultHttpContext { User = principal };

            var controller = new TicketsController(
                Contexto,
                UserManager.Object,
                Localizer.Object,
                UsuarioService.Object,
                Relogio);

            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            controller.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());

            return controller;
        }

        public void Dispose()
        {
            Contexto.Dispose();
            _conexao.Dispose();
        }
    }
}
