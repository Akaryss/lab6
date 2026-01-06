using System;
using System.Collections.Generic;

namespace AdvertisementServiceMVC2.Models
{
    public partial class Advertisement
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }

        // Внешние ключи (обязательные)
        public string UserId { get; set; } = null!;
        public int CategoryId { get; set; }
        public int RegionId { get; set; }

        // Навигационные свойства (делаем nullable для API)
        public virtual Category? Category { get; set; }
        public virtual Region? Region { get; set; }
        public virtual AppUser? User { get; set; }

        public virtual ICollection<AdvertisementPhoto>? Photos { get; set; }
        public virtual ICollection<Message>? Messages { get; set; }
    }
}