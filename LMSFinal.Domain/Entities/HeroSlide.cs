using LMSFinal.Domain.Common;

namespace LMSFinal.Domain.Entities
{
    /// <summary>Слайд карусели на главной странице — управляется администратором.</summary>
    public class HeroSlide : BaseEntity
    {
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>Куда ведёт клик по слайду — необязательно (например, категория курсов).</summary>
        public string? LinkUrl { get; set; }

        public int OrderIndex { get; set; }
    }
}
