namespace ChamadosTi.ViewModels.Sla
{
        public class PerformanceAnalistaDTO
    {
        public string NomeAnalista { get; set; } = null!;
        public int ChamadosAssumidos { get; set; }
        public int ChamadosResolvidos { get; set; }
        public double TaxaAcertoSla { get; set; }
    }
}