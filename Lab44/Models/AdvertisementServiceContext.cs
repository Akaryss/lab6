using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AdvertisementServiceMVC2.Models
{
    public class AdvertisementServiceContext : IdentityDbContext<AppUser>
    {
        public AdvertisementServiceContext(DbContextOptions<AdvertisementServiceContext> options)
            : base(options) { }

        public virtual DbSet<Region> Regions => Set<Region>();
        public virtual DbSet<Category> Categories => Set<Category>();
        public virtual DbSet<Advertisement> Advertisements => Set<Advertisement>();
        public virtual DbSet<AdvertisementPhoto> AdvertisementPhotos => Set<AdvertisementPhoto>();
        public virtual DbSet<Message> Messages => Set<Message>();
        public virtual DbSet<Favorite> Favorites => Set<Favorite>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb); // Обязательно для Identity!

            // --- 1. Настройка таблицы Избранное (Favorites) ---

            // Указываем составной первичный ключ (ИМЕННО ЭТОГО НЕ ХВАТАЛО)
            mb.Entity<Favorite>()
                .HasKey(f => new { f.UserId, f.AdvertisementId });

            // Отключаем каскадное удаление для Юзера, чтобы избежать ошибки SQL Server (Multiple Cascade Paths)
            mb.Entity<Favorite>()
                .HasOne(f => f.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- 2. Настройка Сообщений (Messages) ---

            // Связь "От кого"
            mb.Entity<Message>()
                .HasOne(m => m.FromUser)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Связь "Кому"
            mb.Entity<Message>()
                .HasOne(m => m.ToUser)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- 3. Настройка Категорий (SubCategories) ---
            mb.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.ChildCategories)
                .HasForeignKey(c => c.ParentCategoryID)
                .OnDelete(DeleteBehavior.Restrict);

            // --- 4. Значения по умолчанию ---
            mb.Entity<Advertisement>()
                .Property(a => a.Status)
                .HasDefaultValue("Active");

            mb.Entity<AppUser>()
                .Property(u => u.Rating)
                .HasDefaultValue(5.0m);
        }
    }
}