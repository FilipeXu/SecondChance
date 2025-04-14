using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SecondChance.Data;
using SecondChance.Models;
using SecondChance.ViewModels;
using System;
using System.Threading.Tasks;

namespace SecondChance.Controllers
{
    /// <summary>
    /// Controlador responsável pela gestão de denúncias de utilizadores.
    /// Implementa funcionalidades para reportar comportamentos inadequados.
    /// </summary>
    [Authorize]
    public class ReportController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Construtor do ReportController.
        /// </summary>
        /// <param name="userManager">Gestor de utilizadores para acesso a funcionalidades de identidade</param>
        /// <param name="context">Contexto da base de dados</param>
        public ReportController(UserManager<User> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        /// <summary>
        /// Apresenta o formulário para denunciar um utilizador.
        /// </summary>
        /// <param name="id">ID do utilizador a ser denunciado</param>
        /// <returns>Vista com formulário de denúncia</returns>
        public async Task<IActionResult> ReportUser(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var reportedUser = await _userManager.FindByIdAsync(id);
            if (reportedUser == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (reportedUser.Id == currentUser.Id)
                return BadRequest("Não pode reportar a si mesmo.");

            var viewModel = new ReportUserViewModel
            {
                ReportedUserId = reportedUser.Id,
                ReportedUserName = reportedUser.FullName
            };

            return View(viewModel);
        }

        /// <summary>
        /// Processa o formulário de denúncia de um utilizador.
        /// </summary>
        /// <param name="viewModel">Dados da denúncia</param>
        /// <returns>Redireciona para a página de perfil após submissão bem-sucedida</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportUser(ReportUserViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return View(viewModel);

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return RedirectToAction("Login", "Account");
            
            var reportedUser = await _userManager.FindByIdAsync(viewModel.ReportedUserId);
            if (reportedUser == null)
                return NotFound();

            if (reportedUser.Id == currentUser.Id)
                return BadRequest("não pode reportar a si mesmo.");

            try
            {
                var report = new UserReport
                {
                    ReportedUserId = reportedUser.Id,
                    ReporterUserId = currentUser.Id,
                    Reason = viewModel.Reason,
                    Details = viewModel.Details,
                    ReportDate = DateTime.Now,
                    IsResolved = false,
                    Resolution = ""
                };
                
                _context.UserReports.Add(report);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "O seu relatório foi enviado e será analisado pela equipa de moderação.";
                return RedirectToPage("/Account/Manage/Index", new { area = "Identity", userId = reportedUser.Id });
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Ocorreu um erro ao enviar seu relatório. Por favor, tente novamente.";
                return View(viewModel);
            }
        }
    }
}