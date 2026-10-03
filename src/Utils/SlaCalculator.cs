using System;

namespace ChamadosTi.Utils
{
    public static class SlaCalculator
    {
        /// <summary>
        /// Calcula a data de vencimento do SLA considerando apenas dias úteis (Segunda a Sexta).
        /// </summary>
        public static DateTime CalcularPrazoDiasUteis(DateTime dataInicio, int horasPrazo)
        {
            DateTime dataResultado = dataInicio;
            int horasRestantes = horasPrazo;

            while (horasRestantes > 0)
            {
                // Adiciona 1 hora por ciclo
                dataResultado = dataResultado.AddHours(1);

                // Se o ciclo cair em um sábado ou domingo, pula o dia avançando para o próximo período útil
                if (dataResultado.DayOfWeek == DayOfWeek.Saturday)
                {
                    dataResultado = dataResultado.AddDays(2); // Pula Sábado e Domingo
                }
                else if (dataResultado.DayOfWeek == DayOfWeek.Sunday)
                {
                    dataResultado = dataResultado.AddDays(1); // Caso inicialize no domingo, pula para segunda
                }

                horasRestantes--;
            }

            return dataResultado;
        }
    }
}
