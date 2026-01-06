using Xunit;
using Microsoft.EntityFrameworkCore;
using AdvertisementServiceMVC2.Models;
using AdvertisementServiceMVC2.Controllers.Api;
using Microsoft.AspNetCore.Mvc;

public class AdvertisementsApiControllerTests
{
    [Fact]
    public async Task GetAdvertisements_ReturnsData()
    {
        // Настройка базы в памяти
        var options = new DbContextOptionsBuilder<AdvertisementServiceContext>()
            .UseInMemoryDatabase(databaseName: "ApiTestDb")
            .Options;

        using (var context = new AdvertisementServiceContext(options))
        {
            // Добавляем тестовые данные
            var cat = new Category { CategoryID = 1, CategoryName = "API Test" };
            context.Categories.Add(cat);
            context.Advertisements.Add(new Advertisement { Id = 100, Title = "API Item", CategoryId = 1, UserId = "1", RegionId = 1 });
            await context.SaveChangesAsync();

            var controller = new AdvertisementsApiController(context);

            // Act
            var result = await controller.GetAds();

            // Assert
            var actionResult = Assert.IsType<ActionResult<IEnumerable<object>>>(result);
            Assert.NotNull(actionResult.Value);
        }
    }
}