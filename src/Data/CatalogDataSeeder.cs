using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
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
                    new Setor { Nome = "Comercial e Vendas", Descricao = "Atendimento ao Cliente, CRM e Expansão", Ti = false, Ativo = true }
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
                    new Tipo { Nome = "Impressora", Comentario = "Impressoras térmicas, laser e multifuncionais", Ativo = true },
                    new Tipo { Nome = "Smartphone", Comentario = "Dispositivos móveis corporativos", Ativo = true }
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

                if (!await context.Categorias.AnyAsync())
                {
                    context.Categorias.AddRange(new List<Categoria>
                    {
                        new Categoria { Nome = "Problema com Software / Sistema", Descricao = "Erros em aplicativos, lentidão e instalação de programas", EspecialidadeId = espSuporte.Id },
                        new Categoria { Nome = "Falha de Conexão com a Internet / VPN", Descricao = "Instabilidade de rede, cabo desconectado ou falha na VPN", EspecialidadeId = espRedes.Id },
                        new Categoria { Nome = "Falha Física no Equipamento", Descricao = "Notebook não liga, tela quebrada ou teclado travado", EspecialidadeId = espHardware.Id },
                        new Categoria { Nome = "Criação / Reset de Acessos e Contas", Descricao = "Reset de senhas, criação de e-mail e permissões de pastas", EspecialidadeId = espSistemas.Id }
                    });
                    await context.SaveChangesAsync();
                }
            }
            if (!await context.Itens.AnyAsync())
            {
                var dell = await context.Marcas.FirstOrDefaultAsync(m => m.Nome == "Dell");
                var cisco = await context.Marcas.FirstOrDefaultAsync(m => m.Nome == "Cisco");
                var hp = await context.Marcas.FirstOrDefaultAsync(m => m.Nome == "HP");
                var samsung = await context.Marcas.FirstOrDefaultAsync(m => m.Nome == "Samsung");

                var notebook = await context.Tipos.FirstOrDefaultAsync(t => t.Nome == "Notebook");
                var switchTipo = await context.Tipos.FirstOrDefaultAsync(t => t.Nome == "Switch");
                var impressora = await context.Tipos.FirstOrDefaultAsync(t => t.Nome == "Impressora");
                var smartphone = await context.Tipos.FirstOrDefaultAsync(t => t.Nome == "Smartphone");

                var rh = await context.Setores.FirstOrDefaultAsync(s => s.Nome == "Recursos Humanos");
                var fin = await context.Setores.FirstOrDefaultAsync(s => s.Nome == "Financeiro");
                var op = await context.Setores.FirstOrDefaultAsync(s => s.Nome == "Operações");
                var com = await context.Setores.FirstOrDefaultAsync(s => s.Nome == "Comercial e Vendas");

                if (dell != null && notebook != null)
                {
                    if (rh != null) context.Itens.Add(new Item { Nome = "Notebook Dell Latitude 3420", Comentario = "Patrimônio RH-102", TipoId = notebook.Id, MarcaId = dell.Id, SetorId = rh.Id, Ativo = true });
                    if (fin != null) context.Itens.Add(new Item { Nome = "Notebook Dell Vostro 3520", Comentario = "Patrimônio FIN-405", TipoId = notebook.Id, MarcaId = dell.Id, SetorId = fin.Id, Ativo = true });
                    if (com != null) context.Itens.Add(new Item { Nome = "Notebook Dell Latitude 5430 Touch", Comentario = "Patrimônio COM-089 - Equipe de Vendas Externa", TipoId = notebook.Id, MarcaId = dell.Id, SetorId = com.Id, Ativo = true });
                }

                if (samsung != null && smartphone != null && com != null)
                {
                    context.Itens.Add(new Item { Nome = "Smartphone Samsung Galaxy S23 Corporate", Comentario = "Patrimônio COM-TEL-01 - Uso corporativo do supervisor", TipoId = smartphone.Id, MarcaId = samsung.Id, SetorId = com.Id, Ativo = true });
                }

                if (hp != null && impressora != null && op != null)
                {
                    context.Itens.Add(new Item { Nome = "Impressora HP LaserJet Pro M404dn", Comentario = "Patrimônio OP-IMP-03 - Emissão de Notas de Logística", TipoId = impressora.Id, MarcaId = hp.Id, SetorId = op.Id, Ativo = true });
                }

                if (cisco != null && switchTipo != null && setorTi != null)
                {
                    context.Itens.Add(new Item { Nome = "Switch Cisco Catalyst 24P POE", Comentario = "Ativo de Rede - Rack Central da TI", TipoId = switchTipo.Id, MarcaId = cisco.Id, SetorId = setorTi.Id, Ativo = true });
                }
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
                    new Feriado { Nome = "Proclamação da República", Dia = new DateOnly(anoAtual, 11, 15), Ativo = true },
                    new Feriado { Nome = "Natal", Dia = new DateOnly(anoAtual, 12, 25), Ativo = true }
                });
                await context.SaveChangesAsync();
            }

            if (!await context.Horarios.AnyAsync())
            {
                var chavesDias = new List<string> { "1_Segunda", "2_Terca", "3_Quarta", "4_Quinta", "5_Sexta" };
                var todosSetores = await context.Setores.ToListAsync();

                foreach (var chaveDia in chavesDias)
                {
                    var horarioComercial = new Horario
                    {
                        DiaDaSemana = chaveDia,
                        JornadaInicio = new TimeOnly(8, 0),
                        JornadaFim = new TimeOnly(18, 0),
                        AlmocoInicio = new TimeOnly(12, 0),
                        AlmocoFim = new TimeOnly(13, 0),
                        Ativo = true
                    };
                    context.Horarios.Add(horarioComercial);
                    await context.SaveChangesAsync();

                    foreach (var s in todosSetores)
                    {
                        context.SetoresHorarios.Add(new SetorHorario
                        {
                            HorarioId = horarioComercial.Id,
                            SetorId = s.Id,
                            Ativo = true
                        });
                    }
                }
                await context.SaveChangesAsync();
            }
        }
    }
}
