using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChamadosTi.Tests.Services
{
    public class HorarioServiceTests : IDisposable
    {
        private readonly SqliteConnection _conexao;
        private readonly AppDbContext _ctx;
        private readonly HorarioService _servico;

        public HorarioServiceTests()
        {
            _conexao = new SqliteConnection("DataSource=:memory:");
            _conexao.Open();
            _ctx = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conexao).Options);
            _ctx.Database.EnsureCreated();
            _servico = new HorarioService(_ctx, new SetorService(_ctx));
        }

        [Fact]
        public async Task CriarComSetoresAsync_SetorInexistente_NaoPersisteOHorario()
        {
            var horario = new Horario
            {
                DiaDaSemana = "Segunda",
                JornadaInicio = new TimeOnly(8, 0),
                JornadaFim = new TimeOnly(18, 0),
                Ativo = true
            };
            var horariosAntes = await _ctx.Horarios.IgnoreQueryFilters().CountAsync();

            // O setor 99999 não existe: a gravação vinculada deve falhar e desfazer o horário.
            await Assert.ThrowsAnyAsync<Exception>(() => _servico.CriarComSetoresAsync(horario, new[] { 99999 }));

            _ctx.ChangeTracker.Clear();
            Assert.Equal(horariosAntes, await _ctx.Horarios.IgnoreQueryFilters().CountAsync());
        }

        public void Dispose()
        {
            _ctx.Dispose();
            _conexao.Dispose();
        }
    }
}
