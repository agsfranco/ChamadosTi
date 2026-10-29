namespace ChamadosTi.ViewModels.Sla
{
    public class SlaDashboardViewModel
    {
        // Cards de Indicadores Rápidos
        public int TotalBacklog { get; set; }
        public int TotalSlaVencidos { get; set; }
        public double TaxaConformidadeSla { get; set; }
        public string TempoMedioResolucaoFormatado { get; set; } = null!;

        // Dados para os Gráficos (Listas Chave-Valor para o Chart.js)
        public Dictionary<string, int> ChamadosPorStatus { get; set; } = new();
        public Dictionary<string, int> ChamadosPorPrioridade { get; set; } = new();
        public Dictionary<string, int> EvolucaoAberturasUltimosDias { get; set; } = new();
        
        // Tabela Analítica de Performance
        public List<PerformanceAnalistaDTO> PerformanceAnalistas { get; set; } = new();
    }
}
