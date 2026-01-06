using Xunit;
using Microsoft.EntityFrameworkCore;
using AdvertisementServiceMVC2.Models;
using AdvertisementServiceMVC2.Controllers.Api;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdvertisementServiceMVC2.Tests
{
    public class AdvertisementsApiControllerTests
    {
        [Fact]
        public async Task GetAdvertisements_ReturnsData()
        {
            // 1. Настройка InMemory базы данных с уникальным именем
            var options = new DbContextOptionsBuilder<AdvertisementServiceContext>()
                .UseInMemoryDatabase(databaseName: "ApiTestDb_" + Guid.NewGuid().ToString())
                .Options;

            // 2. Создание тестовых данных
            using (var context = new AdvertisementServiceContext(options))
            {
                var cat = new Category { CategoryID = 1, CategoryName = "Test Category" };
                context.Categories.Add(cat);
                
                context.Advertisements.Add(new Advertisement 
                { 
                    Id = 1, 
                    Title = "Test Ad", 
                    Price = 100, 
                    CategoryId = 1, 
                    UserId = "test-user", 
                    RegionId = 1,
                    CreatedAt = DateTime.Now 
                });
                await context.SaveChangesAsync();
            }

            // 3. Тестирование
            using (var context = new AdvertisementServiceContext(options))
            {
                var controller = new AdvertisementsApiController(context);
                
                // Act
                var result = await controller.GetAds(page: 1, pageSize: 10);

                // Assert
                // ИСПРАВЛЕНО: Мы проверяем, что вернулся именно OkObjectResult (статус 200 OK)
                var okResult = Assert.IsType<OkObjectResult>(result);
                
                // Проверяем, что внутри есть данные
                Assert.NotNull(okResult.Value);
                
                // Дополнительно можно проверить структуру (items, total и т.д.)
                // так как наш метод возвращает анонимный объект
                var responseData = okResult.Value;
                Assert.NotNull(responseData.GetType().GetProperty("items"));
            }
        }
    }
}
