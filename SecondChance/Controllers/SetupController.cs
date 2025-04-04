using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondChance.Data;
using SecondChance.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SecondChance.Controllers
{
    public class SetupController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;

        public SetupController(UserManager<User> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> SetAdmin(string email)
        {
            if (string.IsNullOrEmpty(email))
                return Content("É necessário fornecer um email");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return Content($"utilizador com email {email} não encontrado");

            if (user.IsAdmin)
                return Content($"O utilizador {user.FullName} já é administrador");

            user.IsAdmin = true;
            user.IsFirstUser = true;
            
            var result = await _userManager.UpdateAsync(user);
            
            return result.Succeeded
                ? Content($"utilizador {user.FullName} agora é administrador. <a href='/'>Voltar para a página inicial</a> e fazer login novamente.")
                : Content($"Erro ao definir o utilizador como administrador: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        public async Task<IActionResult> FixFirstUser()
        {
            var firstUser = await _context.Users
                .OrderBy(u => u.JoinDate)
                .FirstOrDefaultAsync();

            if (firstUser == null)
                return Content("Nenhum utilizador encontrado no sistema.");

            firstUser.IsAdmin = true;
            firstUser.IsFirstUser = true;
            await _context.SaveChangesAsync();

            return Content($"O utilizador {firstUser.FullName} foi definido como administrador e primeiro utilizador");
        }
    }
}