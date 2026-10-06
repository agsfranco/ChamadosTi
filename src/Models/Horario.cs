using ChamadosTi.Models.Interfaces;

namespace ChamadosTi.Models
{
    public class Horario: ISoftDelete
    {
        public int Id { get; set; }
        public string DiaDaSemana { get; set; } = null!;
        public TimeOnly JornadaInicio { get; set; }
        public TimeOnly JornadaFim { get; set; }
        public TimeOnly? AlmocoInicio { get; set; }
        public TimeOnly? AlmocoFim { get; set; }        
        public bool Ativo { get; set; }
        public virtual ICollection<SetorHorario>? SetoresHorarios { get; set; } = new List<SetorHorario>();
    }
}