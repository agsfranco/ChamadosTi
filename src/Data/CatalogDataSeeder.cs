using ChamadosTi.Models;
using Microsoft.EntityFrameworkCore;

namespace ChamadosTi.Data
{
    public static class CatalogDataSeeder
    {
        public static async Task SeedCatalogAsync(AppDbContext context)
        {
            var setorTi = await context.Setores.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Ti);
            if (setorTi == null)
            {
                setorTi = new Setor { Nome = "Tecnologia da Informação", Descricao = "Departamento de TI e Service Desk", Ti = true, Ativo = true };
                context.Setores.Add(setorTi);
                await context.SaveChangesAsync();
            }

            if (!await context.Setores.AnyAsync(s => s.Nome == "Recursos Humanos"))
            {
                context.Setores.AddRange(new List<Setor>
                {
                    new Setor { Nome = "Recursos Humanos", Descricao = "Gestão de Pessoas e Departamento Pessoal", Ti = false, Ativo = true },
                    new Setor { Nome = "Financeiro", Descricao = "Contabilidade, Fiscal e Contas a Pagar/Receber", Ti = false, Ativo = true },
                    new Setor { Nome = "Operações", Descricao = "Logística, Estoque e Cadeia de Produção", Ti = false, Ativo = true },
                    new Setor { Nome = "Comercial e Vendas", Descricao = "Atendimento ao Cliente e Expansão", Ti = false, Ativo = true }
                });
                await context.SaveChangesAsync();
            }
            if (!await context.Prioridades.AnyAsync())
            {
                context.Prioridades.AddRange(new List<Prioridade>
                {
                    new Prioridade { Nome = "01 - Baixa", TempoRespostaPadrao = 240, TempoResolucaoPadrao = 480, Padrao = false, Ativo = true },
                    new Prioridade { Nome = "02 - Média", TempoRespostaPadrao = 120, TempoResolucaoPadrao = 240, Padrao = true, Ativo = true },
                    new Prioridade { Nome = "03 - Alta", TempoRespostaPadrao = 30, TempoResolucaoPadrao = 120, Padrao = false, Ativo = true },
                    new Prioridade { Nome = "04 - Crítica", TempoRespostaPadrao = 15, TempoResolucaoPadrao = 60, Padrao = false, Ativo = true }
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
                    new Marca { Nome = "Cisco", Ativo = true },
                    new Marca { Nome = "Apple", Ativo = true },
                    new Marca { Nome = "Samsung", Ativo = true }
                });
                await context.SaveChangesAsync();
            }

            if (!await context.Tipos.AnyAsync())
            {
                context.Tipos.AddRange(new List<Tipo>
                {
                    new Tipo { Nome = "Notebook", Comentario = "Computadores portáteis corporativos", Ativo = true },
                    new Tipo { Nome = "Monitor", Comentario = "Telas e displays de vídeo de estações de trabalho", Ativo = true },
                    new Tipo { Nome = "Switch", Comentario = "Ativos de rede de dados e conectividade LAN", Ativo = true },
                    new Tipo { Nome = "Impressora Térmica", Comentario = "Impressoras de etiquetas e logística", Ativo = true },
                    new Tipo { Nome = "Smartphone", Comentario = "Dispositivos móveis corporativos", Ativo = true },
                    new Tipo { Nome = "Desktop", Comentario = "Computadores de mesa e estações fixas", Ativo = true }
                });
                await context.SaveChangesAsync();
            }
            if (!await context.Especialidades.AnyAsync())
            {
                var espSuporte = new Especialidade { Nome = "Suporte ao Usuário", Descricao = "Atendimento Service Desk, Sistemas e Software", Ativo = true };
                var espRedes = new Especialidade { Nome = "Infraestrutura de Rede", Descricao = "Roteamento, Switches, Firewall e Conectividade", Ativo = true };
                var espHardware = new Especialidade { Nome = "Hardware e Ativos", Descricao = "Manutenção física, Diagnósticos e Periféricos", Ativo = true };
                var espSistemas = new Especialidade { Nome = "Sistemas Corporativos", Descricao = "ERP, Bancos de Dados e Aplicações Web", Ativo = true };

                context.Especialidades.AddRange(new List<Especialidade> { espSuporte, espRedes, espHardware, espSistemas });
                await context.SaveChangesAsync();

                if (!await context.DadosTecnicos.AnyAsync())
                {
                    context.DadosTecnicos.AddRange(new List<DadoTecnico>
                    {
                        new DadoTecnico { EspecialidadeId = espSuporte.Id, Nivel = "N1 - Suporte Técnico Júnior", Ativo = true },
                        new DadoTecnico { EspecialidadeId = espSuporte.Id, Nivel = "N2 - Analista de Suporte Pleno", Ativo = true },
                        new DadoTecnico { EspecialidadeId = espRedes.Id, Nivel = "N3 - Engenheiro de Redes Sênior", Ativo = true },
                        new DadoTecnico { EspecialidadeId = espHardware.Id, Nivel = "N2 - Técnico de Hardware Pleno", Ativo = true },
                        new DadoTecnico { EspecialidadeId = espSistemas.Id, Nivel = "N3 - Administrador de Sistemas Sênior", Ativo = true }
                    });
                    await context.SaveChangesAsync();
                }
            }
            if (!await context.Feriados.AnyAsync())
            {
                int anoAtual = DateTime.UtcNow.Year;
                context.Feriados.AddRange(new List<Feriado>
                {
                    new Feriado { Nome = "Confraternização Universal", Dia = new DateOnly(anoAtual, 1, 1), Ativo = true },
                    new Feriado { Nome = "Dia do Trabalhador", Dia = new DateOnly(anoAtual, 5, 1), Ativo = true },
                    new Feriado { Nome = "Independência do Brasil", Dia = new DateOnly(anoAtual, 9, 7), Ativo = true },
                    new Feriado { Nome = "Proclamação da República", Dia = new DateOnly(anoAtual, 11, 15), Ativo = true },
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
