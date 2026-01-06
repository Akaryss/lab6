using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using AdvertisementServiceMVC2.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace AdvertisementServiceMVC2.Controllers
{
    public class AdvertisementsController : Controller
    {
        private readonly AdvertisementServiceContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly IWebHostEnvironment _appEnvironment;

        public AdvertisementsController(AdvertisementServiceContext db, UserManager<AppUser> userManager, IWebHostEnvironment appEnvironment)
        {
            _db = db;
            _userManager = userManager;
            _appEnvironment = appEnvironment;
        }

        // ==========================================
        // 1. СПИСОК ОБЪЯВЛЕНИЙ С ПАНЕЛЬЮ ФИЛЬТРОВ
        // ==========================================
        [ResponseCache(CacheProfileName = "LabCacheProfile")]
        public async Task<IActionResult> Index(FilterViewModel filter)
        {
            // Отключаем кэш при активных фильтрах
            if (filter.Page > 1 ||
                !string.IsNullOrEmpty(filter.SearchString) ||
                filter.CategoryId.HasValue ||
                filter.RegionId.HasValue ||
                filter.MinPrice.HasValue ||
                filter.MaxPrice.HasValue)
            {
                Response.Headers[HeaderNames.CacheControl] = "no-store,no-cache";
            }

            int pageSize = 12; // Количество объявлений на странице

            // Базовый запрос
            var adsQuery = _db.Advertisements
                .Include(a => a.Category)
                .Include(a => a.Region)
                .Include(a => a.User)
                .Include(a => a.Photos)
                .Where(a => a.Status == "Active");

            // --- ФИЛЬТРАЦИЯ ---

            // Поиск
            if (!string.IsNullOrEmpty(filter.SearchString))
            {
                adsQuery = adsQuery.Where(a =>
                    a.Title.Contains(filter.SearchString) ||
                    a.Description.Contains(filter.SearchString));
            }

            // Категория
            if (filter.CategoryId.HasValue)
            {
                adsQuery = adsQuery.Where(a => a.CategoryId == filter.CategoryId);
            }

            // Регион
            if (filter.RegionId.HasValue)
            {
                adsQuery = adsQuery.Where(a => a.RegionId == filter.RegionId);
            }

            // Цена от
            if (filter.MinPrice.HasValue)
            {
                adsQuery = adsQuery.Where(a => a.Price >= filter.MinPrice.Value);
            }

            // Цена до
            if (filter.MaxPrice.HasValue)
            {
                adsQuery = adsQuery.Where(a => a.Price <= filter.MaxPrice.Value);
            }

            // Сортировка
            adsQuery = adsQuery.OrderByDescending(a => a.CreatedAt);

            // --- ПАГИНАЦИЯ ---
            var total = await adsQuery.CountAsync();
            var items = await adsQuery
                .Skip((filter.Page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Заполняем ViewModel
            filter.Advertisements = items;
            filter.Categories = await _db.Categories.OrderBy(c => c.CategoryName).ToListAsync();
            filter.Regions = await _db.Regions.OrderBy(r => r.CityName).ToListAsync();
            filter.TotalCount = total;
            filter.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

            return View(filter);
        }
        // ==========================================
        // 8. РЕДАКТИРОВАНИЕ ОБЪЯВЛЕНИЯ (GET)
        // ==========================================
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var advertisement = await _db.Advertisements
                .Include(a => a.Category)
                .Include(a => a.Region)
                .Include(a => a.Photos)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (advertisement == null)
            {
                return NotFound();
            }

            // Проверка прав
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id;

            if (advertisement.UserId != currentUserId && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "У вас нет прав для редактирования этого объявления.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Загружаем списки для выпадающих меню
            ViewBag.Categories = new SelectList(
                _db.Categories.OrderBy(c => c.CategoryName),
                "CategoryID",
                "CategoryName",
                advertisement.CategoryId);

            ViewBag.Regions = new SelectList(
                _db.Regions.OrderBy(r => r.CityName),
                "RegionID",
                "CityName",
                advertisement.RegionId);

            return View(advertisement);
        }

        // ==========================================
        // 9. РЕДАКТИРОВАНИЕ ОБЪЯВЛЕНИЯ (POST)
        // ==========================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Advertisement ad, IFormFile? uploadedFile)
        {
            // Ищем существующее объявление
            var existingAd = await _db.Advertisements
                .Include(a => a.Photos)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (existingAd == null)
            {
                return NotFound();
            }

            // Проверка прав
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id;

            if (existingAd.UserId != currentUserId && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "У вас нет прав для редактирования этого объявления.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Исключаем системные поля из валидации
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("Category");
            ModelState.Remove("Region");
            ModelState.Remove("Photos");

            if (ModelState.IsValid)
            {
                try
                {
                    // Обновляем поля
                    existingAd.Title = ad.Title;
                    existingAd.Description = ad.Description;
                    existingAd.Price = ad.Price;
                    existingAd.CategoryId = ad.CategoryId;
                    existingAd.RegionId = ad.RegionId;

                    // Обработка загрузки нового фото (если загружено)
                    if (uploadedFile != null && uploadedFile.Length > 0)
                    {
                        // Удаляем старое главное фото
                        var oldMainPhoto = existingAd.Photos.FirstOrDefault(p => p.IsMain);
                        if (oldMainPhoto != null)
                        {
                            // Удаляем файл с диска (опционально)
                            if (!oldMainPhoto.PhotoURL.StartsWith("http"))
                            {
                                var oldFilePath = Path.Combine(_appEnvironment.WebRootPath, oldMainPhoto.PhotoURL.TrimStart('/'));
                                if (System.IO.File.Exists(oldFilePath))
                                {
                                    System.IO.File.Delete(oldFilePath);
                                }
                            }

                            _db.AdvertisementPhotos.Remove(oldMainPhoto);
                        }

                        // Сохраняем новое фото
                        string fileName = Guid.NewGuid() + "_" + uploadedFile.FileName;
                        string relativePath = "/images/" + fileName;
                        string fullPath = Path.Combine(_appEnvironment.WebRootPath, "images", fileName);

                        // Создаем папку, если её нет
                        var directory = Path.GetDirectoryName(fullPath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        using (var fileStream = new FileStream(fullPath, FileMode.Create))
                        {
                            await uploadedFile.CopyToAsync(fileStream);
                        }

                        // Добавляем новое фото
                        existingAd.Photos.Add(new AdvertisementPhoto
                        {
                            PhotoURL = relativePath,
                            IsMain = true
                        });
                    }

                    // Сохраняем изменения
                    _db.Update(existingAd);
                    await _db.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Объявление успешно обновлено!";
                    return RedirectToAction("Index", "Cabinet");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AdvertisementExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Если есть ошибки валидации - загружаем списки заново
            ViewBag.Categories = new SelectList(
                _db.Categories.OrderBy(c => c.CategoryName),
                "CategoryID",
                "CategoryName",
                ad.CategoryId);

            ViewBag.Regions = new SelectList(
                _db.Regions.OrderBy(r => r.CityName),
                "RegionID",
                "CityName",
                ad.RegionId);

            return View(ad);
        }

        // Вспомогательный метод
        private bool AdvertisementExists(int id)
        {
            return _db.Advertisements.Any(e => e.Id == id);
        }
        // ==========================================
        // 2. ЖИВОЙ ПОИСК (ТЕПЕРЬ ИЩЕТ КАТЕГОРИИ)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> SearchSuggestions(string term)
        {
            if (string.IsNullOrEmpty(term)) return Json(new List<string>());

            // Ищем категории, в названии которых есть введенный текст
            var suggestions = await _db.Categories
                .Where(c => c.CategoryName.Contains(term))
                .Select(c => new
                {
                    label = c.CategoryName, // То, что покажем (Например "Собаки")
                    val = c.CategoryID      // ID категории для ссылки
                })
                .Take(5)
                .ToListAsync();

            return Json(suggestions);
        }

        // ==========================================
        // 3. ПРОСМОТР ДЕТАЛЕЙ ОБЪЯВЛЕНИЯ
        // ==========================================
        public async Task<IActionResult> Details(int id)
        {
            var ad = await _db.Advertisements
                .Include(a => a.User)
                .Include(a => a.Category)
                .Include(a => a.Region)
                .Include(a => a.Photos)
                .Include(a => a.Messages)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (ad == null) return NotFound();

            return View(ad);
        }

        // ==========================================
        // 4. ПОДАЧА ОБЪЯВЛЕНИЯ (GET)
        // ==========================================
        [Authorize]
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_db.Categories.OrderBy(c => c.CategoryName), "CategoryID", "CategoryName");
            ViewBag.Regions = new SelectList(_db.Regions.OrderBy(r => r.CityName), "RegionID", "CityName");
            return View();
        }
        [HttpGet]
        [Authorize] // Только для зарегистрированных
        public async Task<IActionResult> GetSellerPhone(int adId)
        {
            var ad = await _db.Advertisements.Include(a => a.User).FirstOrDefaultAsync(a => a.Id == adId);
            if (ad == null || ad.User == null) return NotFound();

            // Возвращаем телефон или заглушку, если не указан
            return Json(new { phone = ad.User.PhoneNumber ?? "Телефон не указан" });
        }
        // ==========================================
        // 6. ИЗМЕНЕНИЕ СТАТУСА ОБЪЯВЛЕНИЯ (POST)
        // ==========================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, string newStatus)
        {
            var advertisement = await _db.Advertisements.FindAsync(id);

            if (advertisement == null)
            {
                return NotFound();
            }

            // Проверяем, что текущий пользователь - владелец объявления или админ
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id;

            if (advertisement.UserId != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Проверяем, что новый статус допустим
            var allowedStatuses = new[] { "Active", "Sold", "Archived" };
            if (!allowedStatuses.Contains(newStatus))
            {
                TempData["ErrorMessage"] = "Недопустимый статус!";
                // Возвращаем на предыдущую страницу
                return Redirect(Request.Headers["Referer"].ToString() ?? "/Cabinet");
            }

            advertisement.Status = newStatus;
            _db.Update(advertisement);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Статус объявления успешно изменен!";

            // ВОТ ИСПРАВЛЕНИЕ: Возвращаем на предыдущую страницу вместо Details
            return Redirect(Request.Headers["Referer"].ToString() ?? "/Cabinet");
        }

        // ==========================================
        // 7. СТРАНИЦА УПРАВЛЕНИЯ ОБЪЯВЛЕНИЕМ (GET)
        // ==========================================
        [Authorize]
        public async Task<IActionResult> Manage(int id)
        {
            var advertisement = await _db.Advertisements
                .Include(a => a.Category)
                .Include(a => a.Region)
                .Include(a => a.User)
                .Include(a => a.Photos)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (advertisement == null)
            {
                return NotFound();
            }

            // Проверка прав
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id;

            if (advertisement.UserId != currentUserId && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "У вас нет прав для управления этим объявлением.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Список допустимых статусов
            ViewBag.StatusList = new SelectList(new[]
            {
        new { Value = "Active", Text = "Активно" },
        new { Value = "Sold", Text = "Продано" },
        new { Value = "Archived", Text = "В архиве" }
    }, "Value", "Text", advertisement.Status);

            return View(advertisement);
        }
        // ==========================================
        // 5. ПОДАЧА ОБЪЯВЛЕНИЯ (POST)
        // ==========================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Advertisement ad, IFormFile? uploadedFile)
        {
            // Исключаем системные поля из валидации
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("Category");
            ModelState.Remove("Region");

            if (ModelState.IsValid)
            {
                // Привязываем к текущему пользователю
                var user = await _userManager.GetUserAsync(User);
                ad.UserId = user.Id;
                ad.CreatedAt = DateTime.Now;
                ad.Status = "Active";

                // Обработка загрузки фото
                if (uploadedFile != null)
                {
                    string path = "/images/" + Guid.NewGuid() + "_" + uploadedFile.FileName; // Генерируем уникальное имя
                    using (var fileStream = new FileStream(_appEnvironment.WebRootPath + path, FileMode.Create))
                    {
                        await uploadedFile.CopyToAsync(fileStream);
                    }

                    ad.Photos = new List<AdvertisementPhoto>
                    {
                        new AdvertisementPhoto { PhotoURL = path, IsMain = true }
                    };
                }
                else
                {
                    // Заглушка
                    ad.Photos = new List<AdvertisementPhoto>
                    {
                        new AdvertisementPhoto { PhotoURL = "https://placehold.co/400x300?text=No+Photo", IsMain = true }
                    };
                }

                _db.Advertisements.Add(ad);
                await _db.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            // Если ошибка валидации - загружаем списки заново
            ViewBag.Categories = new SelectList(_db.Categories, "CategoryID", "CategoryName");
            ViewBag.Regions = new SelectList(_db.Regions, "RegionID", "CityName");
            return View(ad);
        }
    }
}