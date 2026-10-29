using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ChamadosTi.Tests.Helpers
{
    public sealed record DadosDeTeste(
        Setor SetorTi,
        Setor SetorComum,
        Usuario Solicitante,
        Usuario OutroSolicitante,
        Usuario Tecnico,
        Usuario OutroTecnico,
        Prioridade PrioridadePadrao,
        Prioridade PrioridadeAlta,
        Categoria Categoria,
        Item ItemDoSetorComum,
        Item ItemDoSetorTi);

    public static class SeedData
    {
        public static DadosDeTeste Popular(AppDbContext contexto)
        {
            // Os status (Codigo 1 a 6) já vêm do HasData do AppDbContext via EnsureCreated.

            var setorTi = new Setor { Nome = "TI", Ti = true };
            var setorComum = new Setor { Nome = "Financeiro", Ti = false };
            contexto.Setores.AddRange(setorTi, setorComum);
            contexto.SaveChanges();

            // Horários de expediente do setor TI, um por dia útil e sábado/domingo.
            // Ativo precisa ser true: o default do modelo é false e o filtro global ignora registros inativos.
            var chavesDias = new[] { "1_Segunda", "2_Terca", "3_Quarta", "4_Quinta", "5_Sexta", "6_Sabado", "7_Domingo" };
            foreach (var chave in chavesDias)
            {
                var horario = new Horario
                {
                    DiaDaSemana = chave,
                    JornadaInicio = new TimeOnly(8, 0),
                    JornadaFim = new TimeOnly(18, 0),
                    AlmocoInicio = new TimeOnly(12, 0),
                    AlmocoFim = new TimeOnly(13, 0),
                    Ativo = true
                };
                contexto.Horarios.Add(horario);
                contexto.SaveChanges();

                contexto.SetoresHorarios.Add(new SetorHorario
                {
                    SetorId = setorTi.Id,
                    HorarioId = horario.Id,
                    Ativo = true
                });
            }
            contexto.SaveChanges();

            var solicitante = CriarUsuario(contexto, "solicitante", "Solicitante", setorComum.Id);
            var outroSolicitante = CriarUsuario(contexto, "outro-solicitante", "Outro Solicitante", setorComum.Id);
            var tecnico = CriarUsuario(contexto, "tecnico", "Técnico", setorTi.Id);
            var outroTecnico = CriarUsuario(contexto, "outro-tecnico", "Outro Técnico", setorTi.Id);

            var prioridadePadrao = new Prioridade { Nome = "Média", TempoRespostaPadrao = 4, TempoResolucaoPadrao = 24, Padrao = true };
            var prioridadeAlta = new Prioridade { Nome = "Alta", TempoRespostaPadrao = 1, TempoResolucaoPadrao = 8, Padrao = false };
            contexto.Prioridades.AddRange(prioridadePadrao, prioridadeAlta);

            var categoria = new Categoria { Nome = "Hardware" };
            contexto.Categorias.Add(categoria);

            var tipo = new Tipo { Nome = "Notebook" };
            var marca = new Marca { Nome = "Dell" };
            contexto.Tipos.Add(tipo);
            contexto.Marcas.Add(marca);
            contexto.SaveChanges();

            var itemDoSetorComum = new Item { Nome = "Notebook Financeiro 01", TipoId = tipo.Id, MarcaId = marca.Id, SetorId = setorComum.Id };
            var itemDoSetorTi = new Item { Nome = "Notebook TI 01", TipoId = tipo.Id, MarcaId = marca.Id, SetorId = setorTi.Id };
            contexto.Itens.AddRange(itemDoSetorComum, itemDoSetorTi);
            contexto.SaveChanges();

            return new DadosDeTeste(
                setorTi,
                setorComum,
                solicitante,
                outroSolicitante,
                tecnico,
                outroTecnico,
                prioridadePadrao,
                prioridadeAlta,
                categoria,
                itemDoSetorComum,
                itemDoSetorTi);
        }

        /// <summary>
        /// Cria um ticket diretamente no banco, no status informado.
        /// Quando o status for diferente de Aberto, o técnico atribuído é obrigatório na prática.
        /// </summary>
        public static Ticket CriarTicket(
            AppDbContext contexto,
            DadosDeTeste dados,
            TicketStatus status,
            Usuario? solicitante = null,
            Usuario? tecnico = null,
            DateTime? dataAberturaUtc = null)
        {
            var abertura = dataAberturaUtc ?? new DateTime(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);
            var statusEntidade = contexto.Status.Single(s => s.Codigo == (int)status);

            var ticket = new Ticket
            {
                UsuarioId = (solicitante ?? dados.Solicitante).Id,
                UsuarioTecnicoId = tecnico?.Id,
                SetorId = dados.SetorComum.Id,
                CategoriaId = dados.Categoria.Id,
                PrioridadeId = dados.PrioridadePadrao.Id,
                StatusId = statusEntidade.Id,
                DataAbertura = abertura,
                PrazoRespostaSLA = abertura.AddHours(4),
                PrazoResolucaoSLA = abertura.AddHours(24),
                Titulo = "Notebook não liga",
                Comentario = "Notebook não liga desde ontem pela manhã.",
                Ativo = true
            };

            contexto.Tickets.Add(ticket);
            contexto.SaveChanges();
            return ticket;
        }

        public static Usuario CriarUsuario(AppDbContext contexto, string aspNetId, string nome, int? setorId)
        {
            var identity = new IdentityUser
            {
                Id = aspNetId,
                UserName = aspNetId,
                NormalizedUserName = aspNetId.ToUpperInvariant(),
                Email = $"{aspNetId}@teste.local",
                NormalizedEmail = $"{aspNetId}@TESTE.LOCAL".ToUpperInvariant()
            };
            contexto.Users.Add(identity);
            contexto.SaveChanges();

            var usuario = new Usuario
            {
                AspNetUserId = aspNetId,
                Nome = nome,
                SetorId = setorId,
                Ativo = true
            };
            contexto.Usuarios.Add(usuario);
            contexto.SaveChanges();
            return usuario;
        }
    }
}
