using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SecondChance.Data;
using SecondChance.Hubs;
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
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(ApplicationDbContext context, UserManager<User> userManager, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }        
        /// <summary>
        /// Lista todas as conversas do utilizador atual.
        /// </summary>
        /// <returns>View com a lista de conversas do utilizador atual</returns>
        /// <remarks>Este método carrega todas as conversas privadas do utilizador atual</remarks>
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            return View(await GetUserConversations(currentUser.Id));
        }        
        /// <summary>
        /// Exibe uma conversa específica com outro utilizador.
        /// </summary>
        /// <param name="userId">ID do outro utilizador na conversa</param>
        /// <returns>View com a conversa entre o utilizador atual e o utilizador especificado</returns>
        /// <remarks>Este método marca as mensagens não lidas como lidas quando a conversa é aberta</remarks>
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
        /// <summary>
        /// Envia uma nova mensagem para um utilizador específico.
        /// </summary>
        /// <param name="receiverId">ID do utilizador que receberá a mensagem</param>
        /// <param name="content">Conteúdo da mensagem a ser enviada</param>
        /// <returns>Redireciona para a página de conversa com o utilizador recetor</returns>
        /// <remarks>Este método utiliza SignalR para notificar o destinatário em tempo real</remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(string receiverId, string content)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "A mensagem não pode estar vazia.";
                return RedirectToAction(nameof(Conversation), new { userId = receiverId });
            }

            var receiver = await _userManager.FindByIdAsync(receiverId);
            if (receiver == null) return NotFound();

            string conversationId = GetConversationId(currentUser.Id, receiverId);

            var message = new ChatMessage
            {
                Content = content,
                SentAt = DateTime.Now,
                IsRead = false,
                SenderId = currentUser.Id,
                ReceiverId = receiverId,
                ConversationId = conversationId
            };

            _context.Add(message);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.Group(conversationId).SendAsync("ReceiveMessage", 
                currentUser.FullName, 
                currentUser.Id, 
                content, 
                message.SentAt);            
                return RedirectToAction(nameof(Conversation), new { userId = receiverId });
        }
        /// <summary>
        /// Inicia uma nova conversa com um utilizador específico.
        /// </summary>
        /// <param name="userId">ID do utilizador com quem se deseja iniciar a conversa</param>
        /// <returns>Redireciona para a página de conversa com o utilizador especificado</returns>
        public async Task<IActionResult> StartConversation(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return NotFound();
            
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();
            
            if (await _userManager.FindByIdAsync(userId) == null) return NotFound();
              return RedirectToAction(nameof(Conversation), new { userId });
        }        
        /// <summary>
        /// Obtém todas as conversas de um utilizador específico.
        /// </summary>
        /// <param name="userId">ID do utilizador cujas conversas serão recuperadas</param>
        /// <returns>Lista de conversas do utilizador organizadas por data da última mensagem</returns>
        /// <remarks>
        /// Este método obtém todas as conversas privadas do utilizador (excluindo conversas de suporte),
        /// as agrupa por ID de conversa e devolve detalhes sobre cada conversa.
        /// </remarks>
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
        /// <summary>
        /// Gera um ID único para uma conversa entre dois utilizadores.
        /// </summary>
        /// <param name="user1Id">ID do primeiro utilizador</param>
        /// <param name="user2Id">ID do segundo utilizador</param>
        /// <returns>String representando o ID único da conversa</returns>
        /// <remarks>
        /// O ID da conversa é criado ordenando os IDs dos utilizadores alfabeticamente
        /// e depois concatenando-os com um underscore, garantindo que a mesma conversa 
        /// sempre tenha o mesmo ID independentemente da ordem dos utilizadores.
        /// </remarks>
        private string GetConversationId(string user1Id, string user2Id)
        {
            return string.Join("_", new[] { user1Id, user2Id }.OrderBy(id => id));
        }
    }
}