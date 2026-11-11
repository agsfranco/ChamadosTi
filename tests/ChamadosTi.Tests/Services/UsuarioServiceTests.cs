using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using ChamadosTi.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ChamadosTi.Tests.Services
{
    public class UsuarioServiceTests : IDisposable
    {
        private readonly SqliteConnection _conexao;
        private readonly AppDbContext _ctx;
        private readonly UsuarioService _servico;
        private readonly Mock<UserManager<IdentityUser>> _userManager;

        public UsuarioServiceTests()
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

            _servico = new UsuarioService(_ctx, _userManager.Object);
        }

        [Fact]
        public async Task ReativarAsync_UsuarioInativo_ReativaERetornaSucesso()
        {
            _ctx.Users.Add(new IdentityUser { Id = "aspnet-1", UserName = "a@teste.com" });
            _ctx.Usuarios.Add(new Usuario { AspNetUserId = "aspnet-1", Nome = "Ana", Ativo = false });
            await _ctx.SaveChangesAsync();
            _ctx.ChangeTracker.Clear();

            var usuarioId = await _ctx.Usuarios.IgnoreQueryFilters().Select(u => u.Id).SingleAsync();

            var resultado = await _servico.ReativarAsync(usuarioId);

            Assert.Equal(ResultadoOperacaoUsuario.Sucesso, resultado);
            _ctx.ChangeTracker.Clear();
            Assert.True((await _ctx.Usuarios.IgnoreQueryFilters().SingleAsync(u => u.Id == usuarioId)).Ativo);
        }

        [Fact]
        public async Task ReativarAsync_IdInexistente_RetornaNaoEncontrado()
        {
            var resultado = await _servico.ReativarAsync(999);

            Assert.Equal(ResultadoOperacaoUsuario.NaoEncontrado, resultado);
        }

        [Fact]
        public async Task ObterLogadoAsync_UsuarioAutenticado_RetornaUsuarioLocal()
        {
            _ctx.Users.Add(new IdentityUser { Id = "aspnet-2", UserName = "b@teste.com" });
            _ctx.Usuarios.Add(new Usuario { AspNetUserId = "aspnet-2", Nome = "Bruno", Ativo = true });
            await _ctx.SaveChangesAsync();
            _ctx.ChangeTracker.Clear();

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "aspnet-2") }, "teste"));

            var usuario = await _servico.ObterLogadoAsync(principal);

            Assert.NotNull(usuario);
            Assert.Equal("Bruno", usuario!.Nome);
        }

        [Fact]
        public async Task ObterLogadoAsync_SemIdentidade_RetornaNull()
        {
            var anonimo = new ClaimsPrincipal(new ClaimsIdentity());

            Assert.Null(await _servico.ObterLogadoAsync(anonimo));
        }

        public void Dispose()
        {
            _ctx.Dispose();
            _conexao.Dispose();
        }
    }
}
