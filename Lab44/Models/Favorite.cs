using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdvertisementServiceMVC2.Models
{
    public class Favorite
    {
        // Composite Key настраивается в Context, здесь просто свойства
        public string UserId { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual AppUser User { get; set; } = null!;

        public int AdvertisementId { get; set; }

        [ForeignKey("AdvertisementId")]
        public virtual Advertisement Advertisement { get; set; } = null!;
    }
}