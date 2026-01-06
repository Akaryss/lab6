using AdvertisementServiceMVC2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http; // Добавлено для RequestDelegate

namespace AdvertisementServiceMVC2.Middleware
{
    public class DatabaseInitializerMiddleware
    {
        private readonly RequestDelegate _next;
        public DatabaseInitializerMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, AdvertisementServiceContext db)
        {
            db.Database.SetCommandTimeout(300);

            if (!await db.Categories.AnyAsync())
            {
                // ... (весь твой код заполнения базы без изменений) ...
                var regions = new List<Region>
                {
                    new Region { RegionName = "Москва", CityName = "Москва" },
                    new Region { RegionName = "Санкт-Петербург", CityName = "Санкт-Петербург" },
                    new Region { RegionName = "Екатеринбург", CityName = "Екатеринбург" }
                };
                db.Regions.AddRange(regions);
                await db.SaveChangesAsync();

                var catTransport = new Category { CategoryName = "Транспорт" };
                var catRealEstate = new Category { CategoryName = "Недвижимость" };
                var catElectronics = new Category { CategoryName = "Электроника" };
                db.Categories.AddRange(catTransport, catRealEstate, catElectronics);
                await db.SaveChangesAsync();

                var subCats = new List<Category>
                {
                    new Category { CategoryName = "Автомобили", ParentCategoryID = catTransport.CategoryID },
                    new Category { CategoryName = "Квартиры", ParentCategoryID = catRealEstate.CategoryID },
                    new Category { CategoryName = "Телефоны", ParentCategoryID = catElectronics.CategoryID }
                };
                db.Categories.AddRange(subCats);
                await db.SaveChangesAsync();

                var user1 = new AppUser { UserName = "ivan", Email = "i@test.ru", Name = "Иван", RegionId = regions[0].RegionID };
                db.Users.Add(user1);
                await db.SaveChangesAsync();

                var adsList = new List<Advertisement>();
                for (int i = 1; i <= 10; i++) // Уменьшил число для теста
                {
                    adsList.Add(new Advertisement
                    {
                        Title = $"Лот №{i}",
                        Description = "Описание...",
                        Price = 1000 * i,
                        CategoryId = subCats[0].CategoryID,
                        RegionId = regions[0].RegionID,
                        UserId = user1.Id,
                        Status = "Active",
                        CreatedAt = DateTime.Now
                    });
                }
                await db.Advertisements.AddRangeAsync(adsList);
                await db.SaveChangesAsync();
            }

            await _next(context);
        }
    }
}