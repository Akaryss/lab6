using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdvertisementServiceMVC2.Models
{
    public class AdvertisementPhoto
    {
        [Key]
        public int PhotoID { get; set; }

        public int AdvertisementId { get; set; }

        [ForeignKey("AdvertisementId")]
        public virtual Advertisement Advertisement { get; set; } = null!;

        [Required]
        public string PhotoURL { get; set; } = null!;

        public bool IsMain { get; set; }
    }
}