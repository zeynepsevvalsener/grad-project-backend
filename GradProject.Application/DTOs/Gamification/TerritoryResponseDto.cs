namespace GradProject.Application.DTOs.Gamification
{
    /// <summary>
    /// Bölge özet bilgisi (liste vb.)
    /// </summary>
    public class TerritoryResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? RegionCode { get; set; }
        public int DisplayOrder { get; set; }
        public string? IconUrl { get; set; }
        public int OwnershipTargetPercent { get; set; }
        public bool IsActive { get; set; }
    }
}
