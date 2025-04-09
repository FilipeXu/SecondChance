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
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public CommentsController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Comments for a specific profile
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

        // POST: Comments/Create
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

        // GET: Comments/Delete/5
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

        // POST: Comments/Delete/5
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