using SecondChance.Models;
using System.Collections.Generic;

namespace SecondChance.ViewModels
{
    /// <summary>
    /// Modelo de vista para apresentação do painel principal de moderação.
    /// Contém estatísticas e resumos para moderadores e administradores.
    /// </summary>
    public class ModeratorIndexViewModel
    {
        /// <summary>
        /// Número total de utilizadores registados na plataforma
        /// </summary>
        public int TotalUsers { get; set; }
        
        /// <summary>
        /// Número total de denúncias submetidas na plataforma
        /// </summary>
        public int TotalReports { get; set; }
        
        /// <summary>
        /// Número de denúncias pendentes de resolução
        /// </summary>
        public int UnresolvedReports { get; set; }
        
        /// <summary>
        /// Lista das denúncias mais recentes para revisão rápida
        /// </summary>
        public List<UserReport> RecentReports { get; set; }
    }
}