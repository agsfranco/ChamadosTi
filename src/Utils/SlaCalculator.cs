using System;
using System.Collections.Generic;
using System.Linq;
using ChamadosTi.Models;

namespace ChamadosTi.Utils
{
    public static class SlaCalculator
    {
        /// <summary>
        /// Calcula a data de vencimento do SLA (em formato UTC) considerando feriados, 
        /// fins de semana e o horário de expediente/almoço específico do setor de TI.
        /// Retorna o Prazo também em formato UTC.
        /// </summary>
        public static DateTime CalcularPrazoSlaReal(List<DateOnly> todosFeriados,
                                                    List<Horario> horariosSetores,
                                                    DateTime dataAberturaUtc, 
                                                    int horasPrazo)
        {
            // Converte a data de abertura UTC para o fuso local do servidor para bater com as tabelas de horários (TimeOnly/DateOnly)
            DateTime dataAberturaLocal = dataAberturaUtc.ToLocalTime();
            int minutosRestantes = horasPrazo * 60;

            while (minutosRestantes > 0) 
            {
                TimeOnly horaApenas = TimeOnly.FromDateTime(dataAberturaLocal);

                // 1. CHECAGEM DE FINS DE SEMANA: Se for Sábado ou Domingo, avança para a meia-noite do próximo dia
                if (dataAberturaLocal.DayOfWeek == DayOfWeek.Saturday || dataAberturaLocal.DayOfWeek == DayOfWeek.Sunday)
                {
                    dataAberturaLocal = dataAberturaLocal.Date.AddDays(1);
                    continue;
                }

                // 2. CHECAGEM DE FERIADOS: Se a data atual constar na lista de feriados passados por parâmetro, pula o dia
                if (todosFeriados.Any(x => x == DateOnly.FromDateTime(dataAberturaLocal)))
                {
                    dataAberturaLocal = dataAberturaLocal.Date.AddDays(1);
                    continue;
                }

                // Mapeia o DayOfWeek do C# para a chave string ordenada do banco
                string chaveDiaSemana = ObterChaveDiaSemana(dataAberturaLocal.DayOfWeek);

                // Busca o expediente correspondente àquele dia da semana para o setor de TI
                var expedienteHoje = horariosSetores.FirstOrDefault(h => h.DiaDaSemana == chaveDiaSemana);

                // Se a TI não tiver nenhum horário cadastrado para este dia útil, assume-se que não há expediente (pausa o SLA)
                if (expedienteHoje == null)
                {
                    dataAberturaLocal = dataAberturaLocal.Date.AddDays(1);
                    continue;
                }

                // 3. CHECAGEM DE EXPEDIENTE: Se o relógio estiver antes da abertura, pula para o início da jornada
                if (horaApenas < expedienteHoje.JornadaInicio)
                {
                    dataAberturaLocal = dataAberturaLocal.Date.Add(expedienteHoje.JornadaInicio.ToTimeSpan());
                    continue;
                }
                
                // Se o relógio estiver após o fechamento, joga o relógio para a meia-noite do próximo dia
                if (horaApenas >= expedienteHoje.JornadaFim)
                {
                    dataAberturaLocal = dataAberturaLocal.Date.AddDays(1);
                    continue;
                }

                // 4. CHECAGEM DE INTERVALO (ALMOÇO): Se cair dentro da janela de almoço, extrai o .Value de forma segura e avança
                if (horaApenas >= expedienteHoje.AlmocoInicio && horaApenas < expedienteHoje.AlmocoFim)
                {
                    dataAberturaLocal = dataAberturaLocal.Date.Add(expedienteHoje.AlmocoFim.Value.ToTimeSpan());
                    continue;
                }

                // Se passou por todas as travas e validações, significa que estamos em MINUTO ÚTIL!
                dataAberturaLocal = dataAberturaLocal.AddMinutes(1);
                minutosRestantes--;
            }

            // Converte o resultado local de volta para UTC antes de retornar para persistência segura no banco
            return dataAberturaLocal.ToUniversalTime();
        }

        /// <summary>
        /// Método auxiliar estático para converter o enum DayOfWeek na chave string ordenada do banco de dados.
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
