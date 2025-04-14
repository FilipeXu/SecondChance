using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondChance.Data;
using SecondChance.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SecondChance.Controllers
{
    /// <summary>
    /// Controlador responsável pela gestão de comentários nos perfis de utilizadores.
    /// Implementa funcionalidades para visualizar, criar e eliminar comentários.
    /// </summary>
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        /// <summary>
        /// Construtor do CommentsController.
        /// </summary>
        /// <param name="context">Contexto da base de dados</param>
        /// <param name="userManager">Gestor de utilizadores para acesso a funcionalidades de identidade</param>
        public CommentsController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Apresenta os comentários no perfil de um utilizador específico.
        /// </summary>
        /// <param name="profileId">ID do utilizador cujos comentários serão exibidos</param>
        /// <returns>Vista com a lista de comentários do perfil</returns>
        public async Task<IActionResult> ProfileComments(string profileId)
        {
            var profile = await _userManager.FindByIdAsync(profileId);
            if (profile == null)
            {
                return NotFound();
            }

            var comments = await _context.Comments
                .Include(c => c.Author)
                .Where(c => c.ProfileId == profileId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewData["ProfileId"] = profileId;
            ViewData["ProfileName"] = profile.FullName;

            return View(comments);
        }

        /// <summary>
        /// Processa a criação de um novo comentário num perfil.
        /// </summary>
        /// <param name="profileId">ID do utilizador que receberá o comentário</param>
        /// <param name="content">Conteúdo do comentário</param>
        /// <returns>Redireciona para a lista de comentários do perfil</returns>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string profileId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "O comentário não pode estar vazio.";
                return RedirectToAction("ProfileComments", new { profileId });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var profile = await _userManager.FindByIdAsync(profileId);
            if (profile == null)
            {
                return NotFound();
            }

            var comment = new Comment
            {
                Content = content,
                CreatedAt = DateTime.Now,
                AuthorId = currentUser.Id,
                ProfileId = profileId,
                Author = currentUser,
                Profile = profile
            };

            _context.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("ProfileComments", new { profileId });
        }

        /// <summary>
        /// Apresenta a página de confirmação para eliminar um comentário.
        /// </summary>
        /// <param name="id">ID do comentário a ser eliminado</param>
        /// <returns>Vista de confirmação de eliminação do comentário</returns>
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.Comments
                .Include(c => c.Author)
                .Include(c => c.Profile)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (comment == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            if (comment.AuthorId != currentUser.Id && comment.ProfileId != currentUser.Id)
            {
                return Forbid();
            }

            return View(comment);
        }

        /// <summary>
        /// Processa a eliminação de um comentário.
        /// Apenas o autor do comentário ou o dono do perfil podem eliminar o comentário.
        /// </summary>
        /// <param name="id">ID do comentário a ser eliminado</param>
        /// <returns>Redireciona para a lista de comentários do perfil</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            if (comment.AuthorId != currentUser.Id && comment.ProfileId != currentUser.Id)
            {
                return Forbid();
            }

            string profileId = comment.ProfileId;
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            
            return RedirectToAction("ProfileComments", new { profileId });
        }
    }
}