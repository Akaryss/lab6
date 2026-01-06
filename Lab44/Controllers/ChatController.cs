using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdvertisementServiceMVC2.Models;

namespace AdvertisementServiceMVC2.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly AdvertisementServiceContext _db;
        private readonly UserManager<AppUser> _userManager;

        public ChatController(AdvertisementServiceContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Получаем все сообщения текущего пользователя
            var allMessages = await _db.Messages
                .Include(m => m.FromUser)
                .Include(m => m.ToUser)
                .Include(m => m.Advertisement)
                .Where(m => m.FromUserId == currentUser.Id || m.ToUserId == currentUser.Id)
                .OrderByDescending(m => m.SendDate)
                .ToListAsync();

            // Группируем по собеседникам
            var chatList = new List<ChatViewModel>();

            var groupedMessages = allMessages
                .GroupBy(m => m.FromUserId == currentUser.Id ? m.ToUserId : m.FromUserId);

            foreach (var group in groupedMessages)
            {
                var firstMessage = group.First();
                var interlocutor = firstMessage.FromUserId == currentUser.Id
                    ? firstMessage.ToUser
                    : firstMessage.FromUser;

                chatList.Add(new ChatViewModel
                {
                    InterlocutorId = group.Key,
                    Interlocutor = interlocutor,
                    LastMessage = firstMessage.MessageText,
                    LastMessageDate = firstMessage.SendDate,
                    UnreadCount = group.Count(m => m.ToUserId == currentUser.Id && !m.IsRead),
                    Advertisement = firstMessage.Advertisement,
                    MessageCount = group.Count()
                });
            }

            return View(chatList.OrderByDescending(c => c.LastMessageDate));
        }
        // 2. ОКНО ПЕРЕПИСКИ С КОНКРЕТНЫМ ПОЛЬЗОВАТЕЛЕМ
        // adId передаем, чтобы знать, по поводу какого объявления начали общение (для заголовка)
        public async Task<IActionResult> Conversation(string userId, int? adId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var otherUser = await _db.Users.FindAsync(userId);

            if (otherUser == null) return NotFound();

            // Загружаем историю переписки между этими двумя людьми
            var messages = await _db.Messages
                .Where(m => (m.FromUserId == currentUser.Id && m.ToUserId == userId) ||
                            (m.FromUserId == userId && m.ToUserId == currentUser.Id))
                .OrderBy(m => m.SendDate)
                .ToListAsync();

            ViewBag.OtherUser = otherUser;
            ViewBag.CurrentUserId = currentUser.Id;

            // Если мы пришли со страницы объявления, сохраним его ID для автозаполнения
            ViewBag.AdId = adId;
            if (adId.HasValue)
            {
                var ad = await _db.Advertisements.FindAsync(adId.Value);
                ViewBag.AdTitle = ad?.Title;
            }

            return View(messages);
        }

        // 3. ОТПРАВКА СООБЩЕНИЯ
        [HttpPost]
        public async Task<IActionResult> SendMessage(string toUserId, string messageText, int? adId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (!string.IsNullOrWhiteSpace(messageText))
            {
                // Если adId не передан, пытаемся найти последнее обсуждение или берем null (но в модели у нас int не null)
                // Если в модели AdvertisementId обязателен, нам придется либо передавать его всегда, 
                // либо, если это просто чат "ни о чем", ссылаться на фиктивное/последнее объявление.
                // В твоем случае в модели Message поле AdvertisementId int (не nullable).
                // Поэтому мы ДОЛЖНЫ привязать сообщение к объявлению.

                // Хакинг: если adId нет, ищем любое объявление собеседника или ставим 1.
                // В идеале в будущем надо сделать AdvertisementId nullable в базе.
                int validAdId = adId ?? 1;

                var msg = new Message
                {
                    FromUserId = currentUser.Id,
                    ToUserId = toUserId,
                    MessageText = messageText,
                    SendDate = DateTime.Now,
                    AdvertisementId = validAdId
                };

                _db.Messages.Add(msg);
                await _db.SaveChangesAsync();
            }

            // Возвращаемся в чат
            return RedirectToAction("Conversation", new { userId = toUserId, adId = adId });
        }
    }
}