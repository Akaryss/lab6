using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdvertisementServiceMVC2.Models
{
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; }

        // --- НОВЫЕ ПОЛЯ ДЛЯ ИЕРАРХИИ ---

        // ID родителя (может быть null, если это главная категория)
        public int? ParentCategoryID { get; set; }

        // Ссылка на объект родителя
        [ForeignKey("ParentCategoryID")]
        public virtual Category? ParentCategory { get; set; }

        // Список дочерних категорий (подкатегорий)
        public virtual ICollection<Category> ChildCategories { get; set; } = new List<Category>();

        // --------------------------------

        public virtual ICollection<Advertisement> Advertisements { get; set; } = new List<Advertisement>();
    }
}