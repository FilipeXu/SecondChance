using SecondChance.Models;

namespace SecondChance.ViewModels
{
    /// <summary>
    /// Modelo de vista para apresentação de estatísticas da plataforma.
    /// Contém métricas e dados agregados sobre utilizadores, produtos e doações.
    /// </summary>
    public class StatisticsViewModel
    {
        /// <summary>
        /// Número total de produtos registados na plataforma
        /// </summary>
        public int TotalProducts { get; set; }
        
        /// <summary>
        /// Número total de utilizadores registados
        /// </summary>
        public int TotalUsers { get; set; }
        
        /// <summary>
        /// Número total de produtos que foram doados
        /// </summary>
        public int TotalDonatedProducts { get; set; }
        
        /// <summary>
        /// Tempo médio entre a publicação e a doação de um produto (em dias)
        /// </summary>
        public double AverageTimeToDonation { get; set; }
        
        /// <summary>
        /// Número total de utilizadores banidos ou desativados
        /// </summary>
        public int TotalBannedUsers { get; set; }
        
        /// <summary>
        /// Número total de denúncias submetidas
        /// </summary>
        public int TotalReports { get; set; }
        
        /// <summary>
        /// Estatísticas semanais de produtos adicionados
        /// </summary>
        public Dictionary<string, int> WeeklyDonationStats { get; set; }
        
        /// <summary>
        /// Estatísticas mensais de produtos adicionados
        /// </summary>
        public Dictionary<string, int> MonthlyDonationStats { get; set; }
        
        /// <summary>
        /// Estatísticas semanais de produtos doados
        /// </summary>
        public Dictionary<string, int> WeeklyDonatedStats { get; set; }
        
        /// <summary>
        /// Estatísticas mensais de produtos doados
        /// </summary>
        public Dictionary<string, int> MonthlyDonatedStats { get; set; }
        
        /// <summary>
        /// Estatísticas de produtos por categoria
        /// </summary>
        public Dictionary<string, int> CategoryStats { get; set; }
    }
}