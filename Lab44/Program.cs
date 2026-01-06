using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AdvertisementServiceMVC2.Models;
using AdvertisementServiceMVC2.Data;
using Microsoft.AspNetCore.Mvc;
// ДОБАВЛЕНО: Подключаем папку с Middleware, если она в отдельном namespace
using AdvertisementServiceMVC2.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 1. Настройка MVC + Именованный профиль кэширования
builder.Services.AddControllersWithViews(options =>
{
    options.CacheProfiles.Add("LabCacheProfile", new CacheProfile
    {
        Duration = 290,
        Location = ResponseCacheLocation.Any
    });
});

// 2. Подключение БД
builder.Services.AddDbContext<AdvertisementServiceContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Настройка Swagger (Сервисы)
builder.Services.AddEndpointsApiExplorer();
// В Program.cs найди builder.Services.AddSwaggerGen() и замени на это:
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "My API", Version = "v1" });

    // Этот фильтр говорит Swagger: "Показывай только то, что начинается на /api"
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        return apiDesc.RelativePath != null && apiDesc.RelativePath.Contains("api/");
    });
});

// 4. Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequiredLength = 4;
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<AdvertisementServiceContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

var app = builder.Build();

// --- НАСТРОЙКА КОНВЕЙЕРА (Middleware) ---

// Пункт 1.4: Инициализация БД
app.UseMiddleware<DatabaseInitializerMiddleware>();

// ДОБАВЛЕНО: Включаем Swagger (Пункт 1 задания Лабы 6)
// Теперь API будет задокументировано
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = "swagger"; // Swagger будет доступен по адресу /swagger
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Важно для API контроллеров
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Advertisements}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();