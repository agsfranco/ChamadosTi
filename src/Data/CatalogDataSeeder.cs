using ChamadosTi.Models;
using Microsoft.EntityFrameworkCore;

namespace ChamadosTi.Data
{
    public static class CatalogDataSeeder
    {
        public static async Task SeedCatalogAsync(AppDbContext context)
        {
            var setorTi = await context.Setores.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Ti);
            if (setorTi == null) return;

            if (!await context.Setores.AnyAsync(s => s.Nome == "Recursos Humanos"))
            {
                context.Setores.AddRange(new List<Setor>
                {
                    new Setor { Nome = "Recursos Humanos", Descricao = "Gestão de Pessoas e DP", Ti = false, Ativo = true },
                    new Setor { Nome = "Financeiro", Descricao = "Contabilidade e Contas", Ti = false, Ativo = true },
                    new Setor { Nome = "Operações", Descricao = "Logística e Produção", Ti = false, Ativo = true }
                });
                await context.SaveChangesAsync();
            }

            if (!await context.Prioridades.AnyAsync())
            {
                context.Prioridades.AddRange(new List<Prioridade>
                {
                    new Prioridade { Nome = "Baixa", TempoRespostaPadrao = 240, TempoResolucaoPadrao = 480, Padrao = false, Ativo = true },
                    new Prioridade { Nome = "Média", TempoRespostaPadrao = 120, TempoResolucaoPadrao = 240, Padrao = true, Ativo = true },
                    new Prioridade { Nome = "Alta", TempoRespostaPadrao = 30, TempoResolucaoPadrao = 120, Padrao = false, Ativo = true },
                    new Prioridade { Nome = "Crítica", TempoRespostaPadrao = 15, TempoResolucaoPadrao = 60, Padrao = false, Ativo = true }
                });
                await context.SaveChangesAsync();
            }

            if (!await context.Marcas.AnyAsync())
            {
                context.Marcas.AddRange(new List<Marca>
                {
                    new Marca { Nome = "Dell", Ativo = true },
                    new Marca { Nome = "HP", Ativo = true },
                    new Marca { Nome = "Lenovo", Ativo = true },
                    new Marca { Nome = "Cisco", Ativo = true }
                });
                await context.SaveChangesAsync();
            }

            if (!await context.Tipos.AnyAsync())
            {
                context.Tipos.AddRange(new List<Tipo>
                {
                    new Tipo { Nome = "Notebook", Comentario = "Computadores portáteis corporativos", Ativo = true },
                    new Tipo { Nome = "Monitor", Comentario = "Telas e displays de vídeo", Ativo = true },
                    new Tipo { Nome = "Switch", Comentario = "Ativos de rede de dados", Ativo = true }
                });
                await context.SaveChangesAsync();
            }

            if (!await context.Especialidades.AnyAsync())
            {
                context.Especialidades.AddRange(new List<Especialidade>
                {
                    new Especialidade { Nome = "Suporte ao Usuário", Descricao = "Atendimento N1 e Software", Ativo = true },
                    new Especialidade { Nome = "Infraestrutura de Rede", Descricao = "Atendimento N2, Switches e Roteadores", Ativo = true },
                    new Especialidade { Nome = "Hardware", Descricao = "Manutenção física de ativos", Ativo = true }
                });
                await context.SaveChangesAsync();
            }

            if (!await context.Feriados.AnyAsync())
            {
                int anoAtual = DateTime.UtcNow.Year;
                context.Feriados.AddRange(new List<Feriado>
                {
                    new Feriado { Nome = "Confraternização Universal", Dia = new DateOnly(anoAtual, 1, 1), Ativo = true },
                    new Feriado { Nome = "Dia do Trabalhador", Dia = new DateOnly(anoAtual, 5, 1), Ativo = true },
                    new Feriado { Nome = "Independência do Brasil", Dia = new DateOnly(anoAtual, 9, 7), Ativo = true },
                    new Feriado { Nome = "Natal", Dia = new DateOnly(anoAtual, 12, 25), Ativo = true }
                });
                await context.SaveChangesAsync();
            }

            if (!await context.Horarios.AnyAsync())
            {
                for (int i = 1; i <= 5; i++)
                {
                    var diaEnum = (DayOfWeek)i;

                    var horarioComercial = new Horario
                    {
                        DiaDaSemana = diaEnum.ToString(),
                        JornadaInicio = new TimeOnly(8, 0),
                        JornadaFim = new TimeOnly(18, 0),
                        AlmocoInicio = new TimeOnly(12, 0),
                        AlmocoFim = new TimeOnly(13, 0),
                        Ativo = true
                    };
                    context.Horarios.Add(horarioComercial);
                    await context.SaveChangesAsync();

                    context.SetoresHorarios.Add(new SetorHorario
                    {
                        HorarioId = horarioComercial.Id,
                        SetorId = setorTi.Id,
                        Ativo = true
                    });
                }
                await context.SaveChangesAsync();
            }
        }
    }
}
