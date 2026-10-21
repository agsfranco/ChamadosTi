namespace ChamadosTi.ViewModels.Ticket
{
    public class TicketHistoricoAtivoDTO
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = null!;
        public string DataAbertura { get; set; } = null!;
        public string StatusNome { get; set; } = null!;
        public string? ParecerTecnico { get; set; }
        public List<int> ItemIds { get; set; } = new List<int>(); // Adicione esta propriedade
    }
}
