using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondChance.Data;
using SecondChance.Models;
using SecondChance.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecondChance.Controllers
{
    public class SupportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public SupportController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<bool> IsAdmin()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            return currentUser != null && currentUser.IsAdmin;
        }

        public IActionResult Index()
        {
            var faqs = new List<SupportFAQ>
            {
                new SupportFAQ { 
                    Question = "Como faço para doar um produto?", 
                    Answer = "Para doar um produto, faça login na sua conta, clique em 'Produtos' no menu superior e depois em 'Adicionar Produto'. Preencha o formulário com os detalhes do seu produto e envie."
                },
                new SupportFAQ { 
                    Question = "Posso editar ou remover minha doação?", 
                    Answer = "Sim, pode editar ou remover suas doações a qualquer momento. Vá para a página do produto, para o seu perfil ou para a página de pesquisa, onde verá opções para editar ou excluir o item."
                },
                new SupportFAQ { 
                    Question = "Como entro em contato com o doador?", 
                    Answer = "Para entrar em contato com o doador, acesse a página do produto desejado e clique no botão 'Contactar o Doador'. Isso iniciará uma conversa privada."
                },
                new SupportFAQ { 
                    Question = "Como posso denunciar um utilizador?", 
                    Answer = "Para denunciar um utilizador, vá até o perfil dele e clique no botão 'Reportar o Utilizador'. Preencha o formulário com os detalhes do motivo da denúncia."
                },
                new SupportFAQ { 
                    Question =  "Como posso avaliar um utilizador?",
                    Answer = "Vá até ao perfil de um utilizador e clique nas estrelas para avaliar o utilizador."
                },
                new SupportFAQ { 
                    Question = "Como posso alterar minha senha?", 
                    Answer = "Para alterar sua senha, vá até ao perfil, clique em editar perfil e clique em dados privados. Siga as instruções para redefinir sua senha."
                },
            };

            return View(faqs);
        }

        [Authorize]
        public async Task<IActionResult> Chat()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var supportChatId = $"support_{currentUser.Id}";
            
            var messages = await _context.ChatMessages
                .Where(m => m.ConversationId == supportChatId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var unreadMessages = messages
                .Where(m => m.ReceiverId == currentUser.Id && !m.IsRead);
                
            if (unreadMessages.Any())
            {
                foreach (var message in unreadMessages)
                    message.IsRead = true;
                
                await _context.SaveChangesAsync();
            }

            ViewData["CurrentUserId"] = currentUser.Id;
            ViewData["CurrentUserName"] = currentUser.FullName;
            
            return View(messages);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(string content)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "A mensagem não pode estar vazia.";
                return RedirectToAction(nameof(Chat));
            }

            var supportChatId = $"support_{currentUser.Id}";
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            
            if (!admins.Any())
            {
                _context.Add(new ChatMessage
                {
                    SenderId = currentUser.Id,
                    ReceiverId = currentUser.Id,
                    Content = content,
                    SentAt = DateTime.Now,
                    IsRead = true,
                    ConversationId = supportChatId
                });
            }
            else
            {
                foreach (var admin in admins)
                {
                    _context.Add(new ChatMessage
                    {
                        SenderId = currentUser.Id,
                        ReceiverId = admin.Id,
                        Content = content,
                        SentAt = DateTime.Now,
                        IsRead = false,
                        ConversationId = supportChatId
                    });
                }
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Chat));
        }

        [Authorize]
        public async Task<IActionResult> AdminDashboard()
        {
            if (!await IsAdmin())
                return RedirectToAction("AccessDenied", "Account", new { area = "Identity" });
            
            var supportChats = await _context.ChatMessages
                .Where(m => m.ConversationId.StartsWith("support_"))
                .GroupBy(m => m.ConversationId)
                .Select(g => new SupportChatViewModel
                {
                    ConversationId = g.Key,
                    UserId = g.Key.Substring(8),
                    LastMessageTime = g.Max(m => m.SentAt),
                    UnreadCount = g.Count(m => !m.IsRead && m.ReceiverId == _userManager.GetUserId(User))
                })
                .OrderByDescending(c => c.LastMessageTime)
                .ToListAsync();

            foreach (var chat in supportChats)
            {
                var user = await _userManager.FindByIdAsync(chat.UserId);
                if (user != null)
                {
                    chat.UserName = user.FullName;
                    chat.UserImage = user.Image;
                }
            }

            return View(supportChats);
        }

        [Authorize]
        public async Task<IActionResult> RespondToChat(string userId)
        {
            if (!await IsAdmin())
                return RedirectToAction("AccessDenied", "Account", new { area = "Identity" });
            
            if (string.IsNullOrEmpty(userId))
                return NotFound();

            var currentAdmin = await _userManager.GetUserAsync(User);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound();

            var supportChatId = $"support_{userId}";
            
            var messages = await _context.ChatMessages
                .Where(m => m.ConversationId == supportChatId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var unreadMessages = messages
                .Where(m => m.ReceiverId == currentAdmin.Id && !m.IsRead);
                
            if (unreadMessages.Any())
            {
                foreach (var message in unreadMessages)
                    message.IsRead = true;
                
                await _context.SaveChangesAsync();
            }

            ViewData["CurrentAdminId"] = currentAdmin.Id;
            ViewData["CurrentAdminName"] = currentAdmin.FullName;
            ViewData["UserId"] = userId;
            ViewData["UserName"] = user.FullName;

            return View(messages);
        }
        
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendResponse(string userId, string content)
        {
            if (!await IsAdmin())
                return RedirectToAction("AccessDenied", "Account", new { area = "Identity" });
            
            if (string.IsNullOrEmpty(userId) || string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Utilizador inválido ou mensagem vazia.";
                return RedirectToAction(nameof(AdminDashboard));
            }

            var currentAdmin = await _userManager.GetUserAsync(User);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound();

            var supportChatId = $"support_{userId}";

            try
            {
                _context.Add(new ChatMessage
                {
                    SenderId = currentAdmin.Id,
                    ReceiverId = userId,
                    Content = content,
                    SentAt = DateTime.Now,
                    IsRead = false,
                    ConversationId = supportChatId
                });
                
                await _context.SaveChangesAsync();
                TempData["Success"] = "Resposta enviada com sucesso!";
            }
            catch (Exception)
            {
                TempData["Error"] = "Ocorreu um erro ao enviar a mensagem. Tente novamente.";
            }

            return RedirectToAction(nameof(RespondToChat), new { userId });
        }
    }
}