using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AdvertisementServiceMVC2.Models
{
    public class Message
    {
        [Key]
        public int MessageID { get; set; }

        [Required]
        public string MessageText { get; set; } = null!;

        public DateTime SendDate { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false; // ← НОВОЕ ПОЛЕ

        public int AdvertisementId { get; set; }
        [ForeignKey("AdvertisementId")]
        public virtual Advertisement Advertisement { get; set; } = null!;

        // От кого
        public string FromUserId { get; set; } = null!;
        [ForeignKey("FromUserId")]
        public virtual AppUser FromUser { get; set; } = null!;

        // Кому
        public string ToUserId { get; set; } = null!;
        [ForeignKey("ToUserId")]
        public virtual AppUser ToUser { get; set; } = null!;
    }
}