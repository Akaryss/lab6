// Models/FilterViewModel.cs
namespace AdvertisementServiceMVC2.Models
{
    public class FilterViewModel
    {
        public int? CategoryId { get; set; }
        public int? RegionId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string SearchString { get; set; }
        public int Page { get; set; } = 1;

        // Для отображения в представлении
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<Region> Regions { get; set; } = new List<Region>();
        public List<Advertisement> Advertisements { get; set; } = new List<Advertisement>();
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }
}