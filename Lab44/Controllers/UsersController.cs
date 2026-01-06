using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdvertisementServiceMVC2.Models;

namespace AdvertisementServiceMVC2.Controllers
{
    [ResponseCache(CacheProfileName = "LabCacheProfile")]
    public class UsersController : Controller
    {
        private readonly AdvertisementServiceContext _db;

        public UsersController(AdvertisementServiceContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            // Обращаемся к _db.Users (это Identity пользователи)
            var users = await _db.Users
                .Include(u => u.Region)
                .Include(u => u.Advertisements)
                // В AppUser есть свойство Name, которое мы добавили
                .OrderBy(u => u.Name)
                .ToListAsync();

            return View(users);
        }

        // ИСПРАВЛЕНИЕ: ID пользователя в Identity - это string
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _db.Users
                .Include(u => u.Region)
                .Include(u => u.Advertisements)
                    .ThenInclude(a => a.Category)
                .Include(u => u.Advertisements)
                    .ThenInclude(a => a.Photos)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            return View(user);
        }
    }
}