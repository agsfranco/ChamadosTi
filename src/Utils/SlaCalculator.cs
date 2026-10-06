using ChamadosTi.Data;
using Microsoft.EntityFrameworkCore;

namespace ChamadosTi.Utils
{
    public static class SlaCalculator
    {
        /// <summary>
        /// Calcula a data de vencimento do SLA (em formato UTC) considerando feriados, 
        /// fins de semana e o horário de expediente/almoço específico do setor.
        /// </summary>
        public static DateTime CalcularPrazoSlaReal(AppDbContext context, int setorId, DateTime dataAberturaUtc, int horasPrazo)
        {
            // Converte a data de abertura UTC para o fuso local do servidor para bater com as tabelas de horários (TimeOnly/DateOnly)
            DateTime dataAtual = dataAberturaUtc.ToLocalTime();
            int minutosRestantes = horasPrazo * 60; // Converte o prazo de horas para minutos para cálculo cirúrgico

            // Cache em memória para ganho de performance (evita abrir conexões repetidas ao banco dentro do loop)
            var todosFeriados = context.Feriados.Select(f => f.Dia).ToList();
            
            // Busca todos os turnos cadastrados para o setor do chamado
            var horariosSetor = context.SetoresHorarios
                .Include(sh => sh.Horario)
                .Where(sh => sh.SetorId == setorId)
                .Select(sh => sh.Horario)
                .ToList();

            // Loop minuto a minuto para consumir o prazo da TI apenas em momentos úteis
            while (minutosRestantes > 0)
            {
                DateOnly dataApenas = DateOnly.FromDateTime(dataAtual);
                TimeOnly horaApenas = TimeOnly.FromDateTime(dataAtual);

                // 1. CHECAGEM DE DIAS ÚTEIS: Se for Sábado ou Domingo, avança o dia inteiro
                if (dataAtual.DayOfWeek == DayOfWeek.Saturday || dataAtual.DayOfWeek == DayOfWeek.Sunday)
                {
                    dataAtual = dataAtual.Date.AddDays(1); // Vai para a meia-noite do próximo dia
                    continue;
                }

                // 2. CHECAGEM DE FERIADOS: Se a data constar na tabela de feriados, pula o dia inteiro
                if (todosFeriados.Contains(dataApenas))
                {
                    dataAtual = dataAtual.Date.AddDays(1); // Vai para a meia-noite do próximo dia
                    continue;
                }

                // Mapeia o DayOfWeek do C# para a string de chave com prefixo numérico salva no seu banco
                string chaveDiaSemana = ObterChaveDiaSemana(dataAtual.DayOfWeek);

                // Busca o expediente correspondente àquele dia da semana para o setor
                var expedienteHoje = horariosSetor.FirstOrDefault(h => h.DiaDaSemana == chaveDiaSemana);

                // Se o setor não tiver nenhum horário cadastrado para este dia útil, assume-se que não há expediente (pausa o SLA)
                if (expedienteHoje == null)
                {
                    dataAtual = dataAtual.Date.AddDays(1);
                    continue;
                }

                // 3. CHECAGEM DE EXPEDIENTE: Se o relógio estiver antes da abertura ou após o fechamento, avança o tempo
                if (horaApenas < expedienteHoje.JornadaInicio)
                {
                    // Força o relógio a saltar direto para o horário de abertura do setor hoje
                    dataAtual = dataAtual.Date.Add(expedienteHoje.JornadaInicio.ToTimeSpan());
                    continue;
                }
                if (horaApenas >= expedienteHoje.JornadaFim)
                {
                    // Força o relógio a ir para a meia-noite do próximo dia para reiniciar as validações
                    dataAtual = dataAtual.Date.AddDays(1);
                    continue;
                }

                // 4. CHECAGEM DE INTERVALO (ALMOÇO): Se o relógio cair dentro da janela de almoço, pula para o fim do intervalo
                if (horaApenas >= expedienteHoje.AlmocoInicio && horaApenas < expedienteHoje.AlmocoFim)
                {
                    dataAtual = dataAtual.Date.Add(expedienteHoje.AlmocoFim.Value.ToTimeSpan());
                    continue;
                }

                // Se passou por todas as travas e validações, significa que estamos em MINUTO ÚTIL!
                dataAtual = dataAtual.AddMinutes(1);
                minutosRestantes--;
            }

            // Converte o resultado local de volta para UTC antes de persistir com segurança no banco de dados
            return dataAtual.ToUniversalTime();
        }

        /// <summary>
        /// Método auxiliar para converter o enum DayOfWeek na chave string ordenada do banco de dados.
        /// </summary>
        private static string ObterChaveDiaSemana(DayOfWeek dia)
        {
            return dia switch
            {
                DayOfWeek.Monday    => "1_Segunda",
                DayOfWeek.Tuesday   => "2_Terca",
                DayOfWeek.Wednesday => "3_Quarta",
                DayOfWeek.Thursday  => "4_Quinta",
                DayOfWeek.Friday    => "5_Sexta",
                DayOfWeek.Saturday  => "6_Sabado",
                DayOfWeek.Sunday    => "7_Domingo",
                _                   => throw new ArgumentOutOfRangeException()
            };
        }
    }
}