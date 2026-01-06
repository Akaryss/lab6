using AdvertisementServiceMVC2.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AdvertisementServiceMVC2.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(
            AdvertisementServiceContext context,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // Увеличиваем тайм-аут (операция большая)
            context.Database.SetCommandTimeout(600);

            // Отключаем отслеживание изменений для максимальной скорости вставки
            context.ChangeTracker.AutoDetectChangesEnabled = false;

            // ==========================================
            // 1. БАЗОВЫЕ ДАННЫЕ (РОЛИ, РЕГИОНЫ, КАТЕГОРИИ)
            // ==========================================

            // 1.1 Роли
            string[] roles = { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // 1.2 Регионы
            if (!await context.Regions.AnyAsync())
            {
                var regions = new List<Region>
                     {
                    new Region { RegionName = "Минская область", CityName = "Минск" },
                    new Region { RegionName = "Минская область", CityName = "Борисов" },
                    new Region { RegionName = "Минская область", CityName = "Солигорск" },
                    new Region { RegionName = "Минская область", CityName = "Молодечно" },
                    new Region { RegionName = "Гомельская область", CityName = "Гомель" },
                    new Region { RegionName = "Гомельская область", CityName = "Мозырь" },
                    new Region { RegionName = "Гомельская область", CityName = "Жлобин" },
                    new Region { RegionName = "Могилевская область", CityName = "Могилев" },
                    new Region { RegionName = "Могилевская область", CityName = "Бобруйск" },
                    new Region { RegionName = "Могилевская область", CityName = "Осиповичи" },
                    new Region { RegionName = "Витебская область", CityName = "Витебск" },
                    new Region { RegionName = "Витебская область", CityName = "Орша" },
                    new Region { RegionName = "Витебская область", CityName = "Новополоцк" },
                    new Region { RegionName = "Гродненская область", CityName = "Гродно" },
                    new Region { RegionName = "Гродненская область", CityName = "Лида" },
                    new Region { RegionName = "Гродненская область", CityName = "Слоним" },
                    new Region { RegionName = "Брестская область", CityName = "Брест" },
                    new Region { RegionName = "Брестская область", CityName = "Барановичи" },
                    new Region { RegionName = "Брестская область", CityName = "Пинск" }
                };
                await context.Regions.AddRangeAsync(regions);
                await context.SaveChangesAsync();
            }

            // 1.3 Категории
            if (!await context.Categories.AnyAsync())
            {
                async Task AddCat(string name, string[] subs)
                {
                    var main = new Category { CategoryName = name };
                    context.Categories.Add(main);
                    await context.SaveChangesAsync();
                    foreach (var s in subs) context.Categories.Add(new Category { CategoryName = s, ParentCategoryID = main.CategoryID });
                    await context.SaveChangesAsync();
                }

                await AddCat("Транспорт", new[] { "Автомобили", "Мотоциклы", "Спецтехника", "Запчасти" });
                await AddCat("Недвижимость", new[] { "Квартиры", "Комнаты", "Дома, дачи", "Гаражи", "Коммерческая" });
                await AddCat("Электроника", new[] { "Телефоны", "Планшеты", "Ноутбуки", "Компьютеры", "Фототехника" });
                await AddCat("Личные вещи", new[] { "Одежда, обувь", "Часы и украшения", "Товары для детей" });
                await AddCat("Для дома и дачи", new[] { "Бытовая техника", "Мебель", "Ремонт и строительство", "Растения" });
                await AddCat("Хобби и отдых", new[] { "Спорт и отдых", "Книги", "Музыкальные инструменты", "Велосипеды" });
                await AddCat("Животные", new[] { "Собаки", "Кошки", "Птицы", "Аквариум" });
            }

            // ==========================================
            // 2. СОЗДАНИЕ КЛЮЧЕВЫХ ПОЛЬЗОВАТЕЛЕЙ (Админ + Тестер)
            // ==========================================

            var regionMoscow = await context.Regions.FirstOrDefaultAsync(r => r.CityName == "Москва");

            if (await userManager.FindByEmailAsync("admin@test.ru") == null)
            {
                var admin = new AppUser { UserName = "admin@test.ru", Email = "admin@test.ru", Name = "Администратор", RegionId = regionMoscow?.RegionID, EmailConfirmed = true, Rating = 5.0m };
                await userManager.CreateAsync(admin, "Admin123!");
                await userManager.AddToRoleAsync(admin, "Admin");
            }
            if (await userManager.FindByEmailAsync("user@test.ru") == null)
            {
                var user = new AppUser { UserName = "user@test.ru", Email = "user@test.ru", Name = "Тестовый Юзер", RegionId = regionMoscow?.RegionID, EmailConfirmed = true, Rating = 4.0m };
                await userManager.CreateAsync(user, "User123!");
                await userManager.AddToRoleAsync(user, "User");
            }

            // ==========================================
            // 3. МАССОВАЯ ГЕНЕРАЦИЯ 5000 ПОЛЬЗОВАТЕЛЕЙ
            // ==========================================

            int targetUserCount = 5000;
            int currentUserCount = await context.Users.CountAsync();

            if (currentUserCount < targetUserCount)
            {
                Console.WriteLine($"Генерация {targetUserCount} пользователей...");

                var firstNames = new[] { "Александр", "Дмитрий", "Максим", "Сергей", "Андрей", "Алексей", "Артём", "Илья", "Кирилл", "Михаил", "Анна", "Мария", "Елена", "Дарья", "Алина", "Ирина", "Екатерина", "Ольга", "Юлия", "Татьяна" };
                var lastNames = new[] { "Иванов", "Смирнов", "Кузнецов", "Попов", "Васильев", "Петров", "Соколов", "Михайлов", "Новиков", "Фёдоров", "Морозов", "Волков", "Алексеев", "Лебедев", "Семёнов", "Егоров", "Павлов", "Козлов", "Степанов" };

                var allRegions = await context.Regions.ToListAsync();
                var rnd = new Random();
                var usersBuffer = new List<AppUser>();

                // Генерируем один хеш пароля для всех ботов, чтобы не тратить CPU
                var passwordHasher = new PasswordHasher<AppUser>();
                var genericHash = passwordHasher.HashPassword(null, "BotPass123!");

                for (int i = 0; i < targetUserCount; i++)
                {
                    string fName = firstNames[rnd.Next(firstNames.Length)];
                    string lName = lastNames[rnd.Next(lastNames.Length)];
                    string email = $"bot_{Guid.NewGuid().ToString().Substring(0, 8)}@example.com";

                    var botUser = new AppUser
                    {
                        UserName = email,
                        NormalizedUserName = email.ToUpper(),
                        Email = email,
                        NormalizedEmail = email.ToUpper(),
                        EmailConfirmed = true,
                        PasswordHash = genericHash,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        Name = $"{fName} {lName}",
                        Rating = (decimal)(rnd.Next(35, 51)) / 10.0m, // Рейтинг 3.5 - 5.0
                        RegionId = allRegions[rnd.Next(allRegions.Count)].RegionID,
                        PhoneNumber = $"+7 (9{rnd.Next(10, 99)}) {rnd.Next(100, 999)}-{rnd.Next(10, 99)}-{rnd.Next(10, 99)}"
                    };

                    usersBuffer.Add(botUser);

                    if (usersBuffer.Count >= 1000)
                    {
                        await context.Users.AddRangeAsync(usersBuffer);
                        await context.SaveChangesAsync();
                        usersBuffer.Clear();
                        Console.WriteLine($"Создано {i + 1} пользователей...");
                    }
                }
                if (usersBuffer.Count > 0)
                {
                    await context.Users.AddRangeAsync(usersBuffer);
                    await context.SaveChangesAsync();
                }
            }

            // ==========================================
            // 4. ГЕНЕРАЦИЯ ОБЪЯВЛЕНИЙ (3-8 шт на пользователя -> ~25к)
            // ==========================================

            // Если объявлений уже много (больше 10к), не генерируем заново
            if (await context.Advertisements.CountAsync() > 10000)
            {
                // Возвращаем настройки обратно и выходим
                context.ChangeTracker.AutoDetectChangesEnabled = true;
                return;
            }

            Console.WriteLine("Начинаем генерацию объявлений (~5-6 шт на пользователя)...");

            var allUserIds = await context.Users.Select(u => u.Id).ToListAsync();
            var targetCategories = await context.Categories.Where(c => c.ParentCategoryID != null).ToListAsync();
            var allRegionIds = await context.Regions.Select(r => r.RegionID).ToListAsync();

            var adsBuffer = new List<Advertisement>();
            var rndGen = new Random();

            // Словари для заголовков
            var titlesDict = new Dictionary<string, string[]> {
                { "Автомобили", new[] { "Lada Vesta", "Hyundai Solaris", "Kia Rio", "Volkswagen Polo", "Skoda Octavia", "Toyota Camry", "Ford Focus" } },
                { "Квартиры", new[] { "1-к квартира", "2-к квартира", "3-к квартира", "Студия", "Комната", "Евротрешка" } },
                { "Телефоны", new[] { "iPhone 11", "iPhone XR", "Samsung A51", "Xiaomi Redmi 9", "Honor 10", "Poco X3", "Realme GT" } },
                { "Ноутбуки", new[] { "Asus VivoBook", "Acer Aspire", "Lenovo Legion", "HP Pavilion", "MacBook Air", "MSI Modern" } },
                { "Велосипеды", new[] { "Stels Navigator", "Stern Motion", "Merida Big.Nine", "GT Avalanche", "Cube Aim" } },
                { "Мебель", new[] { "Диван угловой", "Шкаф ИКЕА", "Кровать с матрасом", "Стол кухонный", "Стулья (4 шт)" } }
            };

            int totalAdsCreated = 0;

            foreach (var userId in allUserIds)
            {
                // !!! ИЗМЕНЕНИЕ: Генерируем от 3 до 8 объявлений на каждого пользователя
                int adsPerUser = rndGen.Next(3, 9);

                for (int j = 0; j < adsPerUser; j++)
                {
                    var cat = targetCategories[rndGen.Next(targetCategories.Count)];

                    // Подбор названия
                    string baseTitle = cat.CategoryName;
                    if (titlesDict.ContainsKey(cat.CategoryName))
                    {
                        var opts = titlesDict[cat.CategoryName];
                        baseTitle = opts[rndGen.Next(opts.Length)];
                    }

                    // Вариации названия
                    string[] variations = { "Срочно", "Торг", "Новое", "БУ", "Идеальное состояние", "Полный комплект", "На гарантии" };
                    string title = $"{baseTitle} {variations[rndGen.Next(variations.Length)]}";

                    // Цена
                    decimal price;
                    if (cat.CategoryName.Contains("Квартиры")) price = rndGen.Next(2000, 15000) * 1000m;
                    else if (cat.CategoryName.Contains("Авто")) price = rndGen.Next(300, 4000) * 1000m;
                    else price = rndGen.Next(5, 1000) * 100m;

                    var ad = new Advertisement
                    {
                        UserId = userId,
                        CategoryId = cat.CategoryID,
                        RegionId = allRegionIds[rndGen.Next(allRegionIds.Count)],
                        Title = title,
                        Description = $"Продаю {baseTitle}. Состояние отличное. Пользовался бережно. Возможен небольшой торг при осмотре. Звоните с 10 до 22.",
                        Price = price,
                        Status = "Active",
                        CreatedAt = DateTime.Now.AddDays(-rndGen.Next(1, 120)) // За последние 4 месяца
                    };

                    // Фото-заглушка
                    string photoText = Uri.EscapeDataString(baseTitle.Length > 15 ? baseTitle.Substring(0, 10) : baseTitle);
                    ad.Photos = new List<AdvertisementPhoto>
                    {
                        new AdvertisementPhoto { IsMain = true, PhotoURL = $"https://placehold.co/400x300?text={photoText}" }
                    };

                    adsBuffer.Add(ad);
                    totalAdsCreated++;
                }

                // Сохраняем пачками по 2500
                if (adsBuffer.Count >= 2500)
                {
                    await context.Advertisements.AddRangeAsync(adsBuffer);
                    await context.SaveChangesAsync();
                    adsBuffer.Clear();
                    Console.WriteLine($"Всего сохранено объявлений: {totalAdsCreated}...");
                }
            }

            // Досохраняем остатки
            if (adsBuffer.Count > 0)
            {
                await context.Advertisements.AddRangeAsync(adsBuffer);
                await context.SaveChangesAsync();
                Console.WriteLine($"Всего сохранено объявлений: {totalAdsCreated}");
            }

            // Возвращаем настройки
            context.ChangeTracker.AutoDetectChangesEnabled = true;
            Console.WriteLine("--- ГЕНЕРАЦИЯ ЗАВЕРШЕНА ---");
        }
    }
}