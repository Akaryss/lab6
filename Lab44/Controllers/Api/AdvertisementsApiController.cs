using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdvertisementServiceMVC2.Models;

namespace AdvertisementServiceMVC2.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdvertisementsApiController : ControllerBase
    {
        private readonly AdvertisementServiceContext _context;

        public AdvertisementsApiController(AdvertisementServiceContext context)
        {
            _context = context;
        }

        // 1. GET: Получение списка с пагинацией
        [HttpGet]
        public async Task<IActionResult> GetAds(int page = 1, int pageSize = 10)
        {
            var totalItems = await _context.Advertisements.CountAsync();
            var items = await _context.Advertisements
                .Include(a => a.Category)
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new {
                    a.Id,
                    a.Title,
                    a.Price,
                    a.CategoryId,
                    a.UserId,
                    a.RegionId,
                    CategoryName = a.Category != null ? a.Category.CategoryName : "Нет",
                    UserName = a.User != null ? (a.User.Name ?? a.User.UserName) : "Аноним"
                })
                .ToListAsync();

            return Ok(new { items, total = totalItems, page, pageSize });
        }

        // 2. GET {id}: Получение одного для редактирования
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAd(int id)
        {
            var ad = await _context.Advertisements.FindAsync(id);
            if (ad == null) return NotFound();
            return Ok(ad);
        }

        // 1. Измененный метод POST с логированием ошибок
        [HttpPost]
        public async Task<IActionResult> CreateAd([FromBody] Advertisement ad)
        {
            ModelState.Remove("Category");
            ModelState.Remove("User");
            ModelState.Remove("Region");

            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                // Принудительно ставим Id в 0, чтобы база сама сгенерировала новый Identity
                ad.Id = 0;
                ad.CreatedAt = DateTime.Now;
                ad.Status = "Active";

                _context.Advertisements.Add(ad);
                await _context.SaveChangesAsync();
                return Ok(ad);
            }
            catch (Exception ex)
            {
                // Возвращаем подробности ошибки (например, ошибку внешнего ключа)
                var innerError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { error = innerError });
            }
        }

        // 2. Вспомогательный метод для получения ID первого пользователя (для тестов фронтенда)
        [HttpGet("first-user")]
        public async Task<IActionResult> GetFirstUser()
        {
            var user = await _context.Users.Select(u => u.Id).FirstOrDefaultAsync();
            var region = await _context.Regions.Select(r => r.RegionID).FirstOrDefaultAsync();
            return Ok(new { userId = user, regionId = region });
        }

        // 4. PUT: Обновление
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAd(int id, [FromBody] Advertisement ad)
        {
            ModelState.Remove("Category");
            ModelState.Remove("User");
            ModelState.Remove("Region");

            if (id != ad.Id) return BadRequest("ID mismatch");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var dbAd = await _context.Advertisements.FindAsync(id);
            if (dbAd == null) return NotFound();

            // Обновляем поля вручную
            dbAd.Title = ad.Title;
            dbAd.Price = ad.Price;
            dbAd.CategoryId = ad.CategoryId;
            dbAd.UserId = ad.UserId; // Важно сохранить владельца
            dbAd.RegionId = ad.RegionId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 5. DELETE: Удаление
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAd(int id)
        {
            var ad = await _context.Advertisements.FindAsync(id);
            if (ad == null) return NotFound();

            _context.Advertisements.Remove(ad);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Вспомогательный метод для категорий
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories() =>
            Ok(await _context.Categories.Select(c => new { c.CategoryID, c.CategoryName }).ToListAsync());
    }
}