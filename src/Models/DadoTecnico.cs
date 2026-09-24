using ChamadosTi.Models.Interfaces;

namespace ChamadosTi.Models
{
    public class DadoTecnico: ISoftDelete
    {
        public int Id { get; set; }
        public int? EspecialidadeId { get; set; }
        public string Nivel { get; set; } = null!;
        public bool Ativo { get; set; } = true;
        public virtual Especialidade Especialidades { get; set; } = null!;

    }
}