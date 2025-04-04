using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondChance.Data;
using SecondChance.Models;
using SecondChance.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SecondChance.Controllers
{
    [Authorize]
    public class ModeratorController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;

        public ModeratorController(UserManager<User> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        private async Task<bool> IsAdmin()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            return currentUser != null && currentUser.IsAdmin;
        }

        public async Task<IActionResult> Index()
        {
            if (!await IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var model = new ModeratorIndexViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalReports = await _context.UserReports.CountAsync(),
                UnresolvedReports = await _context.UserReports.CountAsync(r => !r.IsResolved),
                RecentReports = await _context.UserReports
                    .Include(r => r.ReportedUser)
                    .Include(r => r.ReporterUser)
                    .Where(r => !r.IsResolved)  
                    .OrderByDescending(r => r.ReportDate)
                    .Take(5)
                    .ToListAsync()
            };

            return View(model);
        }

        public async Task<IActionResult> Users()
        {
            if (!await IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            return View(await _context.Users.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> ToggleAdmin(string userId)
        {
            if (!await IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "ID de utilizador inválido";
                return RedirectToAction(nameof(Users));
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "utilizador não encontrado";
                return RedirectToAction(nameof(Users));
            }

            if (user.IsFirstUser && user.IsAdmin)
            {
                TempData["ErrorMessage"] = "O administrador principal não pode perder privilégios de administrador.";
                return RedirectToAction(nameof(Users));
            }

            if (user.Id == currentUser.Id && user.IsAdmin)
            {
                TempData["ErrorMessage"] = "Não pode remover seus próprios privilégios de administrador.";
                return RedirectToAction(nameof(Users));
            }

            user.IsAdmin = !user.IsAdmin;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                string statusMessage = user.IsAdmin ? "agora é administrador" : "não é mais administrador";
                TempData["SuccessMessage"] = $"O utilizador {user.FullName} {statusMessage}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Falha ao atualizar o status de administrador do utilizador";
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            if (!await IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var currentUser = await _userManager.GetUserAsync(User);
            var targetUser = await _context.Users.FindAsync(userId);

            if (targetUser == null)
                return NotFound();

            if (targetUser.IsFirstUser)
            {
                TempData["ErrorMessage"] = "O administrador principal não pode ser desativado.";
                return RedirectToAction(nameof(Users));
            }

            if (targetUser.Id == currentUser.Id)
            {
                TempData["ErrorMessage"] = "Não pode desativar sua própria conta.";
                return RedirectToAction(nameof(Users));
            }

            targetUser.PermanentlyDisabled = !targetUser.PermanentlyDisabled;
            if (targetUser.PermanentlyDisabled)
            {
                var userProducts = await _context.Products
                    .Where(p => p.OwnerId == targetUser.Id)
                    .ToListAsync();

                _context.Products.RemoveRange(userProducts);
            }

            await _context.SaveChangesAsync();

            string statusMessage = !targetUser.PermanentlyDisabled ? "ativada" : "desativada";
            TempData["SuccessMessage"] = $"A conta de {targetUser.FullName} foi {statusMessage}.";

            return RedirectToAction(nameof(Users));
        }


        public async Task<IActionResult> Reports(string filter = null)
        {
            if (!await IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var reportsQuery = _context.UserReports
                .Include(r => r.ReportedUser)
                .Include(r => r.ReporterUser)
                .Include(r => r.ResolvedBy)
                .AsQueryable();
                
            if (filter == "unresolved")
            {
                reportsQuery = reportsQuery.Where(r => !r.IsResolved);
                ViewData["CurrentFilter"] = "unresolved";
            }
            else if (filter == "resolved")
            {
                reportsQuery = reportsQuery.Where(r => r.IsResolved);
                ViewData["CurrentFilter"] = "resolved";
            }
            
            var reports = await reportsQuery
                .OrderByDescending(r => r.ReportDate)
                .ToListAsync();

            return View(reports);
        }

        public async Task<IActionResult> ReportDetails(int id)
        {
            if (!await IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var report = await _context.UserReports
                .Include(r => r.ReportedUser)
                .Include(r => r.ReporterUser)
                .Include(r => r.ResolvedBy)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
                return NotFound();

            return View(report);
        }

        [HttpPost]
        public async Task<IActionResult> ResolveReport(int id, string resolution, bool deactivateUser = false)
        {
            if (!await IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var report = await _context.UserReports
                .Include(r => r.ReportedUser)
                .FirstOrDefaultAsync(r => r.Id == id);
                
            if (report == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            report.IsResolved = true;
            report.Resolution = resolution;
            report.ResolvedDate = DateTime.Now;
            report.ResolvedById = currentUser.Id;

            if (deactivateUser && report.ReportedUser != null)
            {
                if (report.ReportedUser.IsFirstUser)
                {
                    TempData["ErrorMessage"] = "O administrador principal não pode ser desativado.";
                }
                else if (report.ReportedUser.Id == currentUser.Id)
                {
                    TempData["ErrorMessage"] = "Não pode desativar sua própria conta.";
                }
                else
                {
                    report.ReportedUser.PermanentlyDisabled = true;
                    var userProducts = await _context.Products
                        .Where(p => p.OwnerId == report.ReportedUser.Id)
                        .ToListAsync();

                    _context.Products.RemoveRange(userProducts);
                    
                    report.Resolution = resolution + " | CONTA DESATIVADA";
                    TempData["SuccessMessage"] = "Relatório resolvido e utilizador desativado com sucesso.";
                }
            }
            else
            {
                TempData["SuccessMessage"] = "Relatório resolvido com sucesso.";
            }

            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Reports));
        }
    }
}