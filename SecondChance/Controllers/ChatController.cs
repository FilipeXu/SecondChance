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
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ChatController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            return View(await GetUserConversations(currentUser.Id));
        }

        public async Task<IActionResult> Conversation(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var otherUser = await _userManager.FindByIdAsync(userId);
            if (otherUser == null) return NotFound();

            string conversationId = GetConversationId(currentUser.Id, userId);

            var messages = await _context.ChatMessages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
                
            var unreadMessages = messages.Where(m => m.ReceiverId == currentUser.Id && !m.IsRead);
            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }
            if (unreadMessages.Any()) await _context.SaveChangesAsync();

            ViewData["CurrentUserId"] = currentUser.Id;
            ViewData["OtherUser"] = otherUser;
            ViewData["CurrentUserName"] = currentUser.FullName;
            ViewData["OtherUserName"] = otherUser.FullName;

            return View(messages);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(string receiverId, string content)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "A mensagem não pode estar vazia";
                return RedirectToAction(nameof(Conversation), new { userId = receiverId });
            }

            var receiver = await _userManager.FindByIdAsync(receiverId);
            if (receiver == null) return NotFound();

            _context.Add(new ChatMessage
            {
                Content = content,
                SentAt = DateTime.Now,
                IsRead = false,
                SenderId = currentUser.Id,
                ReceiverId = receiverId,
                ConversationId = GetConversationId(currentUser.Id, receiverId)
            });
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Conversation), new { userId = receiverId });
        }
        
        public async Task<IActionResult> StartConversation(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return NotFound();
            
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();
            
            if (await _userManager.FindByIdAsync(userId) == null) return NotFound();
            
            return RedirectToAction(nameof(Conversation), new { userId });
        }

        public async Task<IActionResult> UnreadMessageCount()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Json(new { count = 0 });

            var count = await _context.ChatMessages
                .CountAsync(m => m.ReceiverId == currentUser.Id && 
                                !m.IsRead && 
                                !m.ConversationId.StartsWith("support_"));

            return Json(new { count });
        }
        
        private async Task<List<ConversationViewModel>> GetUserConversations(string userId)
        {
            var messages = await _context.ChatMessages
                .Where(m => (m.SenderId == userId || m.ReceiverId == userId) && 
                           !m.ConversationId.StartsWith("support_"))
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            return messages
                .GroupBy(m => m.ConversationId)
                .Select(g => {
                    var isReceiver = g.First().SenderId != userId;
                    return new ConversationViewModel
                    {
                        ConversationId = g.Key,
                        OtherUserId = isReceiver ? g.First().SenderId : g.First().ReceiverId,
                        OtherUserName = isReceiver ? g.First().Sender.FullName : g.First().Receiver.FullName,
                        OtherUserImage = isReceiver ? g.First().Sender.Image : g.First().Receiver.Image,
                        LastMessageContent = g.First().Content,
                        LastMessageTime = g.First().SentAt,
                        UnreadCount = g.Count(m => m.ReceiverId == userId && !m.IsRead)
                    };
                })
                .OrderByDescending(c => c.LastMessageTime)
                .ToList();
        }

        private string GetConversationId(string user1Id, string user2Id)
        {
            return string.Join("_", new[] { user1Id, user2Id }.OrderBy(id => id));
        }
    }
}