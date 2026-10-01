using System.ComponentModel.DataAnnotations;

namespace ChamadosTi.ViewModels.Categoria
{
    public class CategoriaViewModel
    {
        public int Id { get; set; }

        // Mantido sem o Required para aceitar nulo, caso o usuário não selecione nenhuma especialidade
        [Display(Name = "Especialidade")]
        public int? EspecialidadeId { get; set; }

        [Required(ErrorMessage = "CampoObrigatorio")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "MinMax")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "MinMax")]
        [Display(Name = "Descricao")]
        public string? Descricao { get; set; }

        public bool Ativo { get; set; } = true;
    }
}
