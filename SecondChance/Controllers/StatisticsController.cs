using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondChance.Data;
using SecondChance.ViewModels;
using System.Globalization;

namespace SecondChance.Controllers
{
    /// <summary>
    /// Controlador responsável pelas estatísticas da plataforma.
    /// Fornece dados agregados sobre produtos, utilizadores e atividade na plataforma.
    /// </summary>
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Construtor do StatisticsController.
        /// </summary>
        /// <param name="context">Contexto da base de dados</param>
        public StatisticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Apresenta a página com estatísticas gerais da plataforma.
        /// Inclui dados sobre produtos, utilizadores, doações e categorias.
        /// </summary>
        /// <returns>Vista com estatísticas da plataforma</returns>
        public async Task<IActionResult> Index()
        {
            var viewModel = new StatisticsViewModel();
            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);

            var products = await _context.Products
                .Include(p => p.User)
                .OrderBy(p => p.PublishDate)
                .ToListAsync();

            viewModel.TotalProducts = products.Count;
            viewModel.TotalUsers = await _context.Users.CountAsync();
            viewModel.TotalDonatedProducts = products.Count(p => p.IsDonated);
            viewModel.TotalBannedUsers = await _context.Users.CountAsync(u => u.PermanentlyDisabled);
            viewModel.TotalReports = await _context.UserReports.CountAsync();

            var donatedProducts = products.Where(p => p.IsDonated && p.DonatedDate.HasValue);
            if (donatedProducts.Any())
            {
                viewModel.AverageTimeToDonation = donatedProducts
                    .Where(p => p.DonatedDate.HasValue)
                    .Average(p => (p.DonatedDate!.Value - p.PublishDate).TotalDays);
            }

            var donationsByDayOfWeek = products
                .GroupBy(p => p.PublishDate.DayOfWeek)
                .ToDictionary(
                    g => new CultureInfo("pt-PT").DateTimeFormat.GetDayName(g.Key),
                    g => g.Count()
                );

            var donatedByDayOfWeek = products
                .Where(p => p.IsDonated)
                .GroupBy(p => p.DonatedDate?.DayOfWeek ?? p.PublishDate.DayOfWeek)
                .ToDictionary(
                    g => new CultureInfo("pt-PT").DateTimeFormat.GetDayName(g.Key),
                    g => g.Count()
                );

            var allDays = Enum.GetValues<DayOfWeek>();
            viewModel.WeeklyDonationStats = new Dictionary<string, int>();
            viewModel.WeeklyDonatedStats = new Dictionary<string, int>();
            foreach (var day in allDays)
            {
                var dayName = new CultureInfo("pt-PT").DateTimeFormat.GetDayName(day);
                viewModel.WeeklyDonationStats[dayName] = donationsByDayOfWeek.GetValueOrDefault(dayName, 0);
                viewModel.WeeklyDonatedStats[dayName] = donatedByDayOfWeek.GetValueOrDefault(dayName, 0);
            }

            var last12Months = Enumerable.Range(0, 12)
                .Select(i => now.AddMonths(-i))
                .ToList();

            viewModel.MonthlyDonationStats = last12Months.ToDictionary(
                date => date.ToString("MMM", new CultureInfo("pt-PT")),
                date => products.Count(p => p.PublishDate.Year == date.Year && p.PublishDate.Month == date.Month)
            );

            viewModel.MonthlyDonatedStats = last12Months.ToDictionary(
                date => date.ToString("MMM", new CultureInfo("pt-PT")),
                date => products.Count(p => p.IsDonated && p.DonatedDate?.Year == date.Year && p.DonatedDate?.Month == date.Month)
            );

            viewModel.CategoryStats = products
                .GroupBy(p => p.Category)
                .Select(g => new { Category = g.Key.ToString(), Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToDictionary(x => x.Category, x => x.Count);

            return View(viewModel);
        }
    }
}