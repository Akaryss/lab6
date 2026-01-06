using System;
using System.Collections.Generic;

namespace AdvertisementServiceMVC2.Tests
{
    // УДАЛИТЕ FilterViewModel, ChatViewModel, ErrorViewModel если они уже есть в основном проекте
    // Оставьте только те модели, которых нет в основном проекте

    // Если нужно создать тестовую модель, отличную от основной
    public class TestFilterViewModel
    {
        public int Page { get; set; } = 1;
        public string? SearchString { get; set; } // Используем nullable
        public int? CategoryId { get; set; }
        public int? RegionId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public List<AdvertisementServiceMVC2.Models.Advertisement>? Advertisements { get; set; }
        public List<AdvertisementServiceMVC2.Models.Category>? Categories { get; set; }
        public List<AdvertisementServiceMVC2.Models.Region>? Regions { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    // Или проще: удалите все модели из TestModels.cs
    // и используйте только модели из основного проекта
}