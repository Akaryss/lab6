using System.ComponentModel.DataAnnotations;

namespace AdvertisementServiceMVC2.Models
{
    public class Region
    {
        [Key]
        public int RegionID { get; set; }

        [Required]
        [StringLength(100)]
        public string RegionName { get; set; }

        [StringLength(100)]
        public string CityName { get; set; } = string.Empty;

        public virtual ICollection<Advertisement> Advertisements { get; set; } = new List<Advertisement>();
        public virtual ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    }
}