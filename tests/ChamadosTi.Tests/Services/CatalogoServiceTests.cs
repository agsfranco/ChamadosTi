using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChamadosTi.Tests.Services
{
    public class CatalogoServiceTests : IDisposable
    {
        private readonly SqliteConnection _conexao;
        private readonly AppDbContext _ctx;
        private readonly TipoService _tipos;

        public CatalogoServiceTests()
        {
            _conexao = new SqliteConnection("DataSource=:memory:");
            _conexao.Open();
            _ctx = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conexao).Options);
            _ctx.Database.EnsureCreated();
            _tipos = new TipoService(_ctx);
        }

        [Fact]
        public async Task ListarAsync_CadastroComNome_OrdenaPorNome()
        {
            await _tipos.CriarAsync(new Tipo { Nome = "Zeta", Ativo = true });
            await _tipos.CriarAsync(new Tipo { Nome = "Alfa", Ativo = true });
            _ctx.ChangeTracker.Clear();

            var lista = await _tipos.ListarAsync(false);

            Assert.Equal(new[] { "Alfa", "Zeta" }, lista.Select(t => t.Nome));
        }

        [Fact]
        public async Task ListarOrdenadosPorNomeAsync_CadastroComNome_OrdenaPorNome()
        {
            await _tipos.CriarAsync(new Tipo { Nome = "Beta", Ativo = true });
            await _tipos.CriarAsync(new Tipo { Nome = "Alfa", Ativo = true });
            _ctx.ChangeTracker.Clear();

            var lista = await _tipos.ListarOrdenadosPorNomeAsync();

            Assert.Equal(new[] { "Alfa", "Beta" }, lista.Select(t => t.Nome));
        }

        public void Dispose()
        {
            _ctx.Dispose();
            _conexao.Dispose();
        }
    }
}
