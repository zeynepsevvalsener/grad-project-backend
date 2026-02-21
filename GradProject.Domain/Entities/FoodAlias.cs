namespace GradProject.Domain.Entities
{
    public class FoodAlias
    {
        public int Id { get; set; }

        public int FoodId { get; set; }
        public Food Food { get; set; } = null!;

        // "tr", "en" (ileride "de", "fr" vs.)
        public string Language { get; set; } = "en";

        // raw alias e.g. "tavuk göğsü"
        public string Alias { get; set; } = null!;

        // normalized for matching e.g. "tavuk gogsu"
        public string NormalizedAlias { get; set; } = null!;
    }
}