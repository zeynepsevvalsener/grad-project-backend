namespace GradProject.Domain.Entities
{
    public class ConsumedFood
    {
        public int Id { get; set; }

        // Owner
        public int UserId { get; set; }

        // Which food from the main DB
        public int FoodId { get; set; }

        // ✅ NEW: Bu ConsumedFood bir Meal'dan üretildiyse MealId dolar (manual eklemelerde null)
        public int? MealId { get; set; }

        // When it was consumed (store as UTC ideally)
        public DateTime ConsumedAt { get; set; }

        // How many grams the user consumed
        public decimal PortionG { get; set; }

        // Navigation
        public User User { get; set; } = null!;
        public Food Food { get; set; } = null!;
        public Meal? Meal { get; set; } // opsiyonel ama faydalı (FK mapping için şart değil)
    }
}