using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdvertisementServiceMVC2.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;

namespace AdvertisementServiceMVC2.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AdvertisementServiceContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(
            AdvertisementServiceContext db,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index() => View();

        // ==========================================
        // 1. МОДЕРАЦИЯ ОБЪЯВЛЕНИЙ (уже была)
        // ==========================================
        public async Task<IActionResult> Ads(int page = 1)
        {
            int pageSize = 20;
            var adsQuery = _db.Advertisements.Include(a => a.User).Include(a => a.Category).Include(a => a.Region).OrderByDescending(a => a.CreatedAt);
            var totalAds = await adsQuery.CountAsync();
            var ads = await adsQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalAds / (double)pageSize);
            return View(ads);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAd(int id)
        {
            var ad = await _db.Advertisements.FindAsync(id);
            if (ad != null) { _db.Advertisements.Remove(ad); await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Ads));
        }

        // ==========================================
        // 2. УПРАВЛЕНИЕ КАТЕГОРИЯМИ (уже была)
        // ==========================================
        public async Task<IActionResult> Categories() => View(await _db.Categories.Include(c => c.ParentCategory).OrderBy(c => c.CategoryName).ToListAsync());

        public IActionResult CreateCategory() { ViewBag.Parents = new SelectList(_db.Categories, "CategoryID", "CategoryName"); return View(); }

        [HttpPost]
        public async Task<IActionResult> CreateCategory(Category category)
        {
            if (ModelState.IsValid) { _db.Categories.Add(category); await _db.SaveChangesAsync(); return RedirectToAction(nameof(Categories)); }
            return View(category);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var cat = await _db.Categories.FindAsync(id);
            if (cat != null) { _db.Categories.Remove(cat); await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Categories));
        }

        // ==========================================
        // 3. УПРАВЛЕНИЕ РЕГИОНАМИ (уже была)
        // ==========================================
        public async Task<IActionResult> Regions() => View(await _db.Regions.OrderBy(r => r.RegionName).ToListAsync());

        public IActionResult CreateRegion() => View();

        [HttpPost]
        public async Task<IActionResult> CreateRegion(Region region)
        {
            if (ModelState.IsValid) { _db.Regions.Add(region); await _db.SaveChangesAsync(); return RedirectToAction(nameof(Regions)); }
            return View(region);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRegion(int id)
        {
            var reg = await _db.Regions.FindAsync(id);
            if (reg != null) { _db.Regions.Remove(reg); await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Regions));
        }

        // ==========================================
        // 4. НОВОЕ: УПРАВЛЕНИЕ ПОЛЬЗОВАТЕЛЯМИ (ДЛЯ ЛАБЫ 5)
        // ==========================================

        // Список всех пользователей
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        // Редактирование пользователя (GET)
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // Редактирование пользователя (POST)
        [HttpPost]
        public async Task<IActionResult> EditUser(string id, string email, string name)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.Email = email;
            user.UserName = email; // Обычно UserName совпадает с Email в Identity
            user.Name = name; // Твое кастомное поле

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded) return RedirectToAction(nameof(Users));

            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(user);
        }

        // Удаление пользователя
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                // При удалении пользователя Identity автоматически удалит связи, 
                // если настроено каскадное удаление, либо можно сначала удалить его объявления.
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction(nameof(Users));
        }
    }
}