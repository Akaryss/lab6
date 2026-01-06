using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdvertisementServiceMVC2.Models
{
    public class AppUser : IdentityUser
    {
        [Display(Name = "Имя пользователя")]
        public string? Name { get; set; }

        [Column(TypeName = "decimal(3, 2)")]
        public decimal Rating { get; set; } = 5.0m;

        // Связь с регионом
        public int? RegionId { get; set; }
        [ForeignKey("RegionId")]
        public virtual Region? Region { get; set; }

        // Навигационные свойства
        public virtual ICollection<Advertisement> Advertisements { get; set; } = new List<Advertisement>();
        public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

        [InverseProperty("FromUser")]
        public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();

        [InverseProperty("ToUser")]
        public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
    }
}