using SecondChance.Models;
using System.Collections.Generic;

namespace SecondChance.ViewModels
{
    public class ModeratorIndexViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalReports { get; set; }
        public int UnresolvedReports { get; set; }
        public List<UserReport> RecentReports { get; set; }
    }
} 