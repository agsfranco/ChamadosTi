using ChamadosTi.Models.Interfaces;

namespace ChamadosTi.Models
{
    public class SetorHorario : ISoftDelete
    {
        public int SetorId { get; set; }
        public int HorarioId { get; set; }
        public bool Ativo { get; set; }
        public Setor Setor { get; set; } = null!;
        public Horario Horario { get; set; } = null!;

    }
}