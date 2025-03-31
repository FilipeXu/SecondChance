using SecondChance.Models;

namespace SecondChance.ViewModels
{
    public class StatisticsViewModel
    {
        public int TotalProducts { get; set; }
        public int TotalUsers { get; set; }
        public int TotalDonatedProducts { get; set; }
        public double AverageTimeToDonation { get; set; }
        public int TotalBannedUsers { get; set; }
        public int TotalReports { get; set; }
        public Dictionary<string, int> WeeklyDonationStats { get; set; }
        public Dictionary<string, int> MonthlyDonationStats { get; set; }
        public Dictionary<string, int> WeeklyDonatedStats { get; set; }
        public Dictionary<string, int> MonthlyDonatedStats { get; set; }
        public Dictionary<string, int> CategoryStats { get; set; }
    }
}