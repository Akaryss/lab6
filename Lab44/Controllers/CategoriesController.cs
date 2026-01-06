using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdvertisementServiceMVC2.Models;

namespace AdvertisementServiceMVC2.Controllers
{
    [ResponseCache(CacheProfileName = "LabCacheProfile")]
    public class CategoriesController : Controller
    {
        private readonly AdvertisementServiceContext _db;

        public CategoriesController(AdvertisementServiceContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var categories = await _db.Categories
                .Include(c => c.Advertisements)
                .OrderBy(c => c.CategoryName) // Исправлено на CategoryName (как в БД)
                .ToListAsync();

            return View(categories);
        }

        public async Task<IActionResult> Details(int id)
        {
            var category = await _db.Categories
                .Include(c => c.Advertisements)
                    .ThenInclude(a => a.User)
                .Include(c => c.Advertisements)
                    .ThenInclude(a => a.Region)
                .Include(c => c.Advertisements)
                    .ThenInclude(a => a.Photos)
                .FirstOrDefaultAsync(c => c.CategoryID == id); // Используем CategoryID

            if (category == null) return NotFound();

            return View(category);
        }
    }
}