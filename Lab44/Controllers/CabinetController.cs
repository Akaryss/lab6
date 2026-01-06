using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdvertisementServiceMVC2.Models;
using System.Security.Claims; // Нужно для User.FindFirstValue

namespace AdvertisementServiceMVC2.Controllers
{
    [Authorize]
    public class CabinetController : Controller
    {
        private readonly AdvertisementServiceContext _db;
        private readonly UserManager<AppUser> _userManager;

        public CabinetController(AdvertisementServiceContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // Вспомогательный метод для получения ID текущего юзера
        private string GetCurrentUserId()
        {
            return _userManager.GetUserId(User);
        }

        // Главная страница кабинета - список МОИХ объявлений
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account"); // На всякий случай

            var myAds = await _db.Advertisements
                .Include(a => a.Category)
                .Include(a => a.Photos) // Желательно подгрузить фото, чтобы показать их в списке
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View(myAds);
        }

        // Список ИЗБРАННОГО
        public async Task<IActionResult> Favorites()
        {
            var userId = GetCurrentUserId();

            var favorites = await _db.Favorites
                .Include(f => f.Advertisement)
                    .ThenInclude(a => a.Photos)
                .Include(f => f.Advertisement)
                    .ThenInclude(a => a.Region)
                .Include(f => f.Advertisement)
                    .ThenInclude(a => a.Category)
                .Where(f => f.UserId == userId)
                .Select(f => f.Advertisement) // Выбираем сами объявления
                .ToListAsync();

            return View(favorites);
        }

        // Удалить свое объявление
        [HttpPost]
        public async Task<IActionResult> DeleteMyAd(int id)
        {
            var userId = GetCurrentUserId();

            // Ищем объявление, проверяя, что оно принадлежит именно этому юзеру
            var ad = await _db.Advertisements
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (ad != null)
            {
                // Удаляем фото (опционально, если не настроен каскад)
                // Удаляем само объявление
                _db.Advertisements.Remove(ad);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Добавить/Удалить из избранного (AJAX метод)
        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int adId)
        {
            var userId = GetCurrentUserId();

            var exists = await _db.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.AdvertisementId == adId);

            if (exists != null)
            {
                _db.Favorites.Remove(exists);
                await _db.SaveChangesAsync();
                return Json(new { success = true, status = "removed" });
            }
            else
            {
                _db.Favorites.Add(new Favorite { UserId = userId, AdvertisementId = adId });
                await _db.SaveChangesAsync();
                return Json(new { success = true, status = "added" });
            }
        }
    }
}