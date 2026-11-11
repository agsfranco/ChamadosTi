using System.Globalization;
using System.Text.Json;
using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using ChamadosTi.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ChamadosTi.Data
{
    public static class CatalogDataSeeder
    {
        private const int SementeChamados = 2026;
        private const int TotalChamadosDemo = 45;

        /// <summary>
        /// Carga inicial completa: catálogo, usuários de demonstração e chamados com histórico coerente.
        /// Os chamados só são gerados em banco sem nenhum chamado, então a carga não altera um banco já em uso.
        /// </summary>
        public static async Task SeedDemonstracaoAsync(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            await SeedCatalogAsync(context);

            if (await context.Tickets.IgnoreQueryFilters().AnyAsync())
            {
                return;
            }

            await SeedUsuariosDemoAsync(context, userManager);
            await SeedChamadosDemoAsync(context);
        }

        public static async Task SeedCatalogAsync(AppDbContext context)
        {
            var setorTi = await GarantirSetorTiAsync(context);
            await SeedSetoresAsync(context);
            await SeedPrioridadesAsync(context);
            await SeedMarcasAsync(context);
            await SeedTiposAsync(context);
            await SeedEspecialidadesEDependentesAsync(context);
            await SeedItensAsync(context, setorTi);
            await SeedFeriadosAsync(context);
            await SeedHorariosAsync(context);
        }

        private static async Task<Setor> GarantirSetorTiAsync(AppDbContext context)
        {
            var setorTi = await context.Setores.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Ti);
            if (setorTi == null)
            {
                setorTi = new Setor { Nome = "Tecnologia da Informação", Descricao = "Departamento de TI e Service Desk", Ti = true, Ativo = true };
                context.Setores.Add(setorTi);
                await context.SaveChangesAsync();
            }
            return setorTi;
        }

        private static async Task SeedSetoresAsync(AppDbContext context)
        {
            if (await context.Setores.AnyAsync(s => s.Nome == "Recursos Humanos")) return;

            context.Setores.AddRange(new List<Setor>
            {
                new Setor { Nome = "Recursos Humanos", Descricao = "Gestão de Pessoas e Departamento Pessoal", Ti = false, Ativo = true },
                new Setor { Nome = "Financeiro", Descricao = "Contabilidade, Fiscal e Contas a Pagar/Receber", Ti = false, Ativo = true },
                new Setor { Nome = "Operações", Descricao = "Logística, Estoque e Cadeia de Produção", Ti = false, Ativo = true },
                new Setor { Nome = "Comercial e Vendas", Descricao = "Atendimento ao Cliente, CRM e Expansão", Ti = false, Ativo = true }
            });
            await context.SaveChangesAsync();
        }

        private static async Task SeedPrioridadesAsync(AppDbContext context)
        {
            if (await context.Prioridades.AnyAsync()) return;

            context.Prioridades.AddRange(new List<Prioridade>
            {
                new Prioridade { Nome = "01 - Baixa", TempoRespostaPadrao = 240, TempoResolucaoPadrao = 480, Padrao = false, Ativo = true },
                new Prioridade { Nome = "02 - Média", TempoRespostaPadrao = 120, TempoResolucaoPadrao = 240, Padrao = true, Ativo = true },
                new Prioridade { Nome = "03 - Alta", TempoRespostaPadrao = 30, TempoResolucaoPadrao = 120, Padrao = false, Ativo = true },
                new Prioridade { Nome = "04 - Crítica", TempoRespostaPadrao = 15, TempoResolucaoPadrao = 60, Padrao = false, Ativo = true }
            });
            await context.SaveChangesAsync();
        }

        private static async Task SeedMarcasAsync(AppDbContext context)
        {
            if (await context.Marcas.AnyAsync()) return;

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

        private static async Task SeedTiposAsync(AppDbContext context)
        {
            if (await context.Tipos.AnyAsync()) return;

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

        // Especialidades, dados técnicos e categorias são carregados juntos, só quando não há especialidades.
        private static async Task SeedEspecialidadesEDependentesAsync(AppDbContext context)
        {
            if (await context.Especialidades.AnyAsync()) return;

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

        private static async Task SeedItensAsync(AppDbContext context, Setor setorTi)
        {
            if (await context.Itens.AnyAsync()) return;

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

            if (cisco != null && switchTipo != null)
            {
                context.Itens.Add(new Item { Nome = "Switch Cisco Catalyst 24P POE", Comentario = "Ativo de Rede - Rack Central da TI", TipoId = switchTipo.Id, MarcaId = cisco.Id, SetorId = setorTi.Id, Ativo = true });
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedFeriadosAsync(AppDbContext context)
        {
            if (await context.Feriados.AnyAsync()) return;

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

        private static async Task SeedHorariosAsync(AppDbContext context)
        {
            if (await context.Horarios.AnyAsync()) return;

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

        // ---------------------------------------------------------------------
        // Usuários de demonstração
        // ---------------------------------------------------------------------

        private sealed record PessoaDemo(string Nome, string Email, string NomeSetor, string? NivelTecnico);

        private static readonly PessoaDemo[] PessoasDemo =
        {
            new("Carla Mendes", "carla.mendes@exemplo.com", "Tecnologia da Informação", "N2 - Analista de Suporte Pleno"),
            new("Rafael Souza", "rafael.souza@exemplo.com", "Tecnologia da Informação", "N3 - Engenheiro de Redes Sênior"),
            new("Helena Ribeiro", "helena.ribeiro@exemplo.com", "Tecnologia da Informação", "N2 - Técnico de Hardware Pleno"),
            new("Marina Lopes", "marina.lopes@exemplo.com", "Financeiro", null),
            new("João Pereira", "joao.pereira@exemplo.com", "Comercial e Vendas", null),
            new("Patrícia Alves", "patricia.alves@exemplo.com", "Recursos Humanos", null),
            new("Bruno Costa", "bruno.costa@exemplo.com", "Operações", null),
        };

        // As contas são criadas com senha aleatória que não é guardada: o acesso se faz pela recuperação de senha.
        private static async Task SeedUsuariosDemoAsync(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            foreach (var pessoa in PessoasDemo)
            {
                var identity = await userManager.FindByEmailAsync(pessoa.Email);
                if (identity == null)
                {
                    identity = new IdentityUser { UserName = pessoa.Email, Email = pessoa.Email, EmailConfirmed = true };
                    var resultado = await userManager.CreateAsync(identity, $"Demo-{Guid.NewGuid():N}-Aa1!");
                    if (!resultado.Succeeded)
                    {
                        throw new InvalidOperationException($"Falha ao criar usuário de demonstração {pessoa.Email}: {string.Join("; ", resultado.Errors.Select(e => e.Description))}");
                    }

                    if (pessoa.NivelTecnico != null)
                    {
                        await userManager.AddToRoleAsync(identity, UsuarioRole.Tecnico.ToString());
                    }
                }

                if (await context.Usuarios.AnyAsync(u => u.AspNetUserId == identity.Id)) continue;

                var setor = await context.Setores.IgnoreQueryFilters().FirstAsync(s => s.Nome == pessoa.NomeSetor);
                var dadoTecnico = pessoa.NivelTecnico == null
                    ? null
                    : await context.DadosTecnicos.FirstAsync(d => d.Nivel == pessoa.NivelTecnico);

                context.Usuarios.Add(new Usuario
                {
                    AspNetUserId = identity.Id,
                    Nome = pessoa.Nome,
                    SetorId = setor.Id,
                    DadoTecnicoId = dadoTecnico?.Id,
                    Ativo = true
                });
            }
            await context.SaveChangesAsync();
        }

        // ---------------------------------------------------------------------
        // Chamados de demonstração
        // ---------------------------------------------------------------------

        private static async Task SeedChamadosDemoAsync(AppDbContext context)
        {
            var setorTi = await context.Setores.FirstOrDefaultAsync(s => s.Ti);
            if (setorTi == null) return;

            var horariosTi = await context.SetoresHorarios
                                          .Include(s => s.Horario)
                                          .Where(s => s.SetorId == setorTi.Id)
                                          .Select(s => s.Horario)
                                          .ToListAsync();
            // Sem expediente de TI o cálculo de SLA não termina; nesse caso não há chamados a gerar.
            if (horariosTi.Count == 0) return;

            var feriados = await context.Feriados.Select(f => f.Dia).ToListAsync();
            var status = await context.Status.ToDictionaryAsync(s => s.Codigo);
            var prioridades = await context.Prioridades.OrderBy(p => p.Id).ToListAsync();
            var categorias = await context.Categorias.Include(c => c.Especialidades).OrderBy(c => c.Id).ToListAsync();
            var itens = await context.Itens.OrderBy(i => i.Id).ToListAsync();
            var usuarios = await context.Usuarios.Include(u => u.DadosTecnicos).OrderBy(u => u.Id).ToListAsync();

            var tecnicos = usuarios.Where(u => u.DadoTecnicoId != null).ToList();
            var solicitantes = usuarios.Where(u => u.DadoTecnicoId == null && u.SetorId != setorTi.Id).ToList();
            if (tecnicos.Count == 0 || solicitantes.Count == 0 || categorias.Count == 0 || prioridades.Count == 0) return;

            var rng = new Random(SementeChamados);
            var agora = DateTime.UtcNow;
            var chamados = new List<Ticket>();

            for (int i = 0; i < TotalChamadosDemo; i++)
            {
                var solicitante = solicitantes[rng.Next(solicitantes.Count)];
                var categoria = categorias[rng.Next(categorias.Count)];
                var tecnico = EscolherTecnico(tecnicos, categoria, rng);
                var prioridade = prioridades[rng.Next(prioridades.Count)];
                var abertura = SortearAbertura(agora, rng);

                var itensDoSetor = itens.Where(it => it.SetorId == solicitante.SetorId).ToList();
                var item = itensDoSetor.Count > 0 && rng.NextDouble() < 0.7 ? itensDoSetor[rng.Next(itensDoSetor.Count)] : null;

                var builder = new ChamadoDemoBuilder(rng, agora, status, tecnico, solicitante, categoria, prioridade, feriados, horariosTi);
                chamados.Add(builder.Montar(abertura, item));
            }

            context.Tickets.AddRange(chamados);
            await context.SaveChangesAsync();
        }

        private static Usuario EscolherTecnico(List<Usuario> tecnicos, Categoria categoria, Random rng)
        {
            var especialistas = tecnicos.Where(t => t.DadosTecnicos?.EspecialidadeId == categoria.EspecialidadeId).ToList();
            var lista = especialistas.Count > 0 ? especialistas : tecnicos;
            return lista[rng.Next(lista.Count)];
        }

        // Abertura entre 0 e 29 dias atrás, em horário comercial (UTC); chamados de hoje podem ter sido abertos há pouco.
        private static DateTime SortearAbertura(DateTime agora, Random rng)
        {
            if (rng.Next(0, 6) == 0)
            {
                return agora.AddMinutes(-rng.Next(45, 300));
            }

            var dia = agora.Date.AddDays(-rng.Next(1, 30));
            return DateTime.SpecifyKind(dia.AddHours(rng.Next(11, 21)).AddMinutes(rng.Next(0, 60)), DateTimeKind.Utc);
        }

        /// <summary>
        /// Monta um chamado e o histórico de auditoria seguindo as mesmas regras das ações do TicketsController:
        /// cada transição grava o log com o mesmo modelo que a aplicação usa, e o estado final corresponde ao último evento.
        /// </summary>
        private sealed class ChamadoDemoBuilder
        {
            private static readonly string[] Cenarios = { "Aberto", "EmAtendimento", "Pendente", "Resolvido", "Cancelado" };

            private readonly Random _rng;
            private readonly DateTime _agora;
            private readonly Dictionary<int, Status> _status;
            private readonly Usuario _tecnico;
            private readonly Usuario _solicitante;
            private readonly Categoria _categoria;
            private readonly Prioridade _prioridade;
            private readonly List<DateOnly> _feriados;
            private readonly List<Horario> _horariosTi;

            public ChamadoDemoBuilder(Random rng, DateTime agora, Dictionary<int, Status> status, Usuario tecnico, Usuario solicitante,
                                      Categoria categoria, Prioridade prioridade, List<DateOnly> feriados, List<Horario> horariosTi)
            {
                _rng = rng;
                _agora = agora;
                _status = status;
                _tecnico = tecnico;
                _solicitante = solicitante;
                _categoria = categoria;
                _prioridade = prioridade;
                _feriados = feriados;
                _horariosTi = horariosTi;
            }

            public Ticket Montar(DateTime abertura, Item? item)
            {
                var (titulo, comentario) = TextoDoChamado(_categoria.Nome);
                var prazoResposta = SlaCalculator.CalcularPrazoSlaReal(_feriados, _horariosTi, abertura, _prioridade.TempoRespostaPadrao);
                var prazoResolucao = SlaCalculator.CalcularPrazoSlaReal(_feriados, _horariosTi, abertura, _prioridade.TempoResolucaoPadrao);

                var ticket = new Ticket
                {
                    UsuarioId = _solicitante.Id,
                    SetorId = _solicitante.SetorId!.Value,
                    CategoriaId = _categoria.Id,
                    PrioridadeId = _prioridade.Id,
                    StatusId = _status[(int)TicketStatus.Aberto].Id,
                    DataAbertura = abertura,
                    PrazoRespostaSLA = prazoResposta,
                    PrazoResolucaoSLA = prazoResolucao,
                    Titulo = titulo,
                    Comentario = comentario,
                    Ativo = true
                };

                if (item != null)
                {
                    ticket.ItensTickets!.Add(new ItemTicket { ItemId = item.Id, Ativo = true });
                }

                ticket.AuditoriaTickets!.Add(Evento(abertura, TicketAcaoLog.Log_AberturaChamado, _solicitante.Nome, null, new Dictionary<string, string>
                {
                    { TicketAcaoLog.Log_StatusInicial.ToString(), TicketAcaoLog.Log_StatusInicial.ToString() },
                    { TicketAcaoLog.Log_PrazoResposta.ToString(), prazoResposta.ToString("g", CultureInfo.InvariantCulture) },
                    { TicketAcaoLog.Log_PrazoResolucao.ToString(), prazoResolucao.ToString("g", CultureInfo.InvariantCulture) }
                }));

                var cenario = SortearCenario();

                if (cenario == "Aberto")
                {
                    InteracaoEntre(ticket, abertura, _agora, _solicitante);
                    return Definir(ticket, TicketStatus.Aberto, null);
                }

                if (cenario == "Cancelado" && _rng.NextDouble() < 0.4)
                {
                    var tCancela = Depois(abertura, 20, 300);
                    if (tCancela == null) return Definir(ticket, TicketStatus.Aberto, null);
                    ticket.AuditoriaTickets!.Add(Evento(tCancela.Value, TicketAcaoLog.Log_ChamadoCancelado, _solicitante.Nome, Justificativa(), CampoStatus("Status_Cancelado")));
                    return Definir(ticket, TicketStatus.Cancelado, tCancela);
                }

                var tAssume = Depois(abertura, 10, 240);
                if (tAssume == null) return Definir(ticket, TicketStatus.Aberto, null);
                ticket.AuditoriaTickets!.Add(Evento(tAssume.Value, TicketAcaoLog.Log_ChamadoAssumido, _tecnico.Nome, null, new Dictionary<string, string>
                {
                    { TicketAcaoLog.Log_StatusAlterado.ToString(), "Status_EmAtendimento" },
                    { TicketAcaoLog.Log_TecnicoAtribuido.ToString(), _tecnico.Nome }
                }));
                ticket.UsuarioTecnicoId = _tecnico.Id;

                if (cenario == "EmAtendimento")
                {
                    InteracaoEntre(ticket, tAssume.Value, _agora, _tecnico);
                    return Definir(ticket, TicketStatus.EmAtendimento, null);
                }

                if (cenario == "Pendente")
                {
                    var tPendente = Depois(tAssume.Value, 30, 180);
                    if (tPendente == null) return Definir(ticket, TicketStatus.EmAtendimento, null);
                    InteracaoEntre(ticket, tAssume.Value, tPendente.Value, _tecnico);
                    ticket.AuditoriaTickets!.Add(Evento(tPendente.Value, TicketAcaoLog.Log_ChamadoPendente, _tecnico.Nome, null, null));
                    return Definir(ticket, TicketStatus.Pendente, tPendente);
                }

                if (cenario == "Cancelado")
                {
                    var tCancela = Depois(tAssume.Value, 30, 240);
                    if (tCancela == null) return Definir(ticket, TicketStatus.EmAtendimento, null);
                    InteracaoEntre(ticket, tAssume.Value, tCancela.Value, _tecnico);
                    ticket.AuditoriaTickets!.Add(Evento(tCancela.Value, TicketAcaoLog.Log_ChamadoCancelado, _tecnico.Nome, Justificativa(), CampoStatus("Status_Cancelado")));
                    return Definir(ticket, TicketStatus.Cancelado, tCancela);
                }

                // Resolvido, com ou sem uma pausa anterior (que estende o prazo de resolução, como na aplicação).
                var tRef = tAssume.Value;
                var houvePausa = _rng.NextDouble() < 0.25;
                if (houvePausa)
                {
                    var tPausa = Depois(tAssume.Value, 30, 120);
                    if (tPausa == null) return Definir(ticket, TicketStatus.EmAtendimento, null);
                    InteracaoEntre(ticket, tAssume.Value, tPausa.Value, _tecnico);
                    ticket.AuditoriaTickets!.Add(Evento(tPausa.Value, TicketAcaoLog.Log_ChamadoPendente, _tecnico.Nome, null, null));

                    var tRetoma = Depois(tPausa.Value, 60, 480);
                    if (tRetoma == null) return Definir(ticket, TicketStatus.Pendente, tPausa);

                    var pausaMinutos = (tRetoma.Value - tPausa.Value).TotalMinutes;
                    var campos = CampoStatus("Status_EmAtendimento");
                    ticket.PrazoResolucaoSLA = ticket.PrazoResolucaoSLA.AddMinutes(pausaMinutos);
                    campos.Add(TicketAcaoLog.Log_PrazoResolucaoEstendido.ToString(), ticket.PrazoResolucaoSLA.ToString("o", CultureInfo.InvariantCulture));
                    ticket.AuditoriaTickets!.Add(Evento(tRetoma.Value, TicketAcaoLog.Log_ChamadoRetomado, _tecnico.Nome, null, campos));
                    tRef = tRetoma.Value;
                }

                var tResolve = Depois(tRef, 30, 360);
                if (tResolve == null) return Definir(ticket, TicketStatus.EmAtendimento, null);
                if (!houvePausa) InteracaoEntre(ticket, tAssume.Value, tResolve.Value, _tecnico);
                ticket.AuditoriaTickets!.Add(Evento(tResolve.Value, TicketAcaoLog.Log_ChamadoResolvido, _tecnico.Nome, Parecer(), CampoStatus("Status_Resolvido")));
                return Definir(ticket, TicketStatus.Resolvido, tResolve);
            }

            private string SortearCenario()
            {
                var sorteio = _rng.NextDouble();
                return sorteio switch
                {
                    < 0.12 => Cenarios[0],
                    < 0.25 => Cenarios[1],
                    < 0.35 => Cenarios[2],
                    < 0.80 => Cenarios[3],
                    _ => Cenarios[4],
                };
            }

            // Próximo instante após "inicio", sorteado no intervalo; null se cair depois de agora (o evento ainda não aconteceu).
            private DateTime? Depois(DateTime inicio, int minMinutos, int maxMinutos)
            {
                var t = inicio.AddMinutes(_rng.Next(minMinutos, maxMinutos + 1));
                return t <= _agora ? t : null;
            }

            // Interação entre duas transições: nunca acontece depois do fechamento nem depois de agora.
            private void InteracaoEntre(Ticket ticket, DateTime inicio, DateTime fim, Usuario autor)
            {
                if (_rng.NextDouble() >= 0.5) return;
                var t = inicio.AddMinutes(_rng.Next(10, 90));
                if (t >= fim || t > _agora) return;
                var mensagem = autor.Id == _tecnico.Id ? MensagemTecnico() : MensagemSolicitante();
                ticket.AuditoriaTickets!.Add(Evento(t, TicketAcaoLog.Log_NovaInteracao, autor.Nome, null,
                    new Dictionary<string, string> { { TicketAcaoLog.Log_NovaInteracao.ToString(), mensagem } }));
            }

            private Ticket Definir(Ticket ticket, TicketStatus estado, DateTime? momento)
            {
                ticket.StatusId = _status[(int)estado].Id;
                if (estado is TicketStatus.Resolvido or TicketStatus.Cancelado)
                {
                    ticket.DataFechamento = momento;
                }
                return ticket;
            }

            private static AuditoriaTicket Evento(DateTime quando, TicketAcaoLog acao, string usuario, string? mensagem, Dictionary<string, string>? campos)
            {
                var log = new LogAuditoriaTicket
                {
                    Acao = acao,
                    Usuario = usuario,
                    CamposModificados = campos,
                    Mensagem = mensagem
                };

                return new AuditoriaTicket
                {
                    Log = JsonSerializer.Serialize(log),
                    RegistradoEm = quando,
                    Ativo = true
                };
            }

            private static Dictionary<string, string> CampoStatus(string statusNome) =>
                new() { { TicketAcaoLog.Log_StatusAlterado.ToString(), statusNome } };

            private string Justificativa() => Pick(new[]
            {
                "Chamado duplicado de outro registro já em andamento.",
                "O solicitante resolveu o problema por conta própria.",
                "Equipamento substituído, o atendimento não é mais necessário.",
            });

            private string Parecer() => Pick(new[]
            {
                "Aplicativo reinstalado e validado com o usuário.",
                "Cabo de rede trocado e conexão testada no ponto.",
                "Equipamento enviado para reparo na garantia do fabricante.",
                "Senha redefinida e acesso liberado após validação de identidade.",
                "Configuração de VPN corrigida e conexão validada.",
            });

            private string MensagemTecnico() => Pick(new[]
            {
                "Preciso de mais detalhes sobre o erro. Pode enviar a mensagem completa que aparece na tela?",
                "Estou analisando o caso, retorno em breve com uma atualização.",
                "Pode me confirmar se o problema persiste após reiniciar o equipamento?",
            });

            private string MensagemSolicitante() => Pick(new[]
            {
                "Segue a mensagem de erro que aparece na tela, em anexo.",
                "Obrigado pelo retorno, o problema continua acontecendo.",
                "Aconteceu de novo hoje no início da manhã.",
            });

            private string Pick(string[] opcoes) => opcoes[_rng.Next(opcoes.Length)];

            private (string Titulo, string Comentario) TextoDoChamado(string nomeCategoria)
            {
                var titulos = nomeCategoria switch
                {
                    "Problema com Software / Sistema" => new[] { ("Sistema financeiro trava ao gerar relatório", "O relatório mensal fecha sem concluir desde a última atualização do sistema."), ("Erro ao abrir planilha de controle", "A planilha compartilhada abre com erro de leitura e perdi o trabalho do dia."), ("Aplicativo fecha sozinho após atualização", "Depois da atualização o aplicativo fecha assim que eu faço o primeiro login.") },
                    "Falha de Conexão com a Internet / VPN" => new[] { ("Sem acesso à internet na estação", "A estação não conecta à internet desde o início do expediente, mesmo com o cabo ligado."), ("VPN desconecta a cada dez minutos", "Trabalho remoto está inviável porque a VPN cai com frequência."), ("Lentidão na rede do setor", "Os arquivos da rede demoram muito para abrir e isso atrasa todo o setor.") },
                    "Falha Física no Equipamento" => new[] { ("Notebook não liga após queda", "Derrubei o notebook na mesa e ele não liga mais, mesmo ligado na tomada."), ("Teclado com teclas falhando", "Algumas teclas não respondem e preciso digitar com cuidado todo o tempo."), ("Tela com listras verticais", "A tela do equipamento apresenta listras que atrapalham a leitura.") },
                    _ => new[] { ("Reset de senha do e-mail corporativo", "Esqueci a senha e a recuperação automática não está funcionando para mim."), ("Acesso à pasta compartilhada do setor", "Preciso de permissão de leitura e escrita na pasta do meu setor."), ("Criação de conta para novo colaborador", "Um novo colaborador entra na próxima semana e precisa de acesso ao sistema.") },
                };
                return titulos[_rng.Next(titulos.Length)];
            }
        }
    }
}
