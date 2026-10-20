namespace ChamadosTi.ViewModels.Usuario
{
    public class UsuarioIndexViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SetorNome { get; set; } = string.Empty;
        public bool EhTecnico { get; set; }
        public bool EhAdmin { get; set; }
        public bool EhAdminMaster { get; set; }
        public bool Ativo { get; set; }
    }
}
