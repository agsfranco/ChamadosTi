using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using ChamadosTi.Services;
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

        public UsuarioServiceTests()
        {
            _conexao = new SqliteConnection("DataSource=:memory:");
            _conexao.Open();
            _ctx = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conexao).Options);
            _ctx.Database.EnsureCreated();

            var userManager = new Mock<UserManager<IdentityUser>>(
                Mock.Of<IUserStore<IdentityUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

            _servico = new UsuarioService(_ctx, userManager.Object);
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

        public void Dispose()
        {
            _ctx.Dispose();
            _conexao.Dispose();
        }
    }
}
