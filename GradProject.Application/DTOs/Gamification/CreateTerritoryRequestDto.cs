using GradProject.Domain.Enums;

namespace GradProject.Application.DTOs.Gamification
{
    /// <summary>
    /// Yeni bölge oluşturma isteği (draft - ileride API'de kullanılabilir).
    /// </summary>
    public class CreateTerritoryRequestDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? RegionCode { get; set; }
        public int DisplayOrder { get; set; }
        public string? IconUrl { get; set; }
        public int OwnershipTargetPercent { get; set; } = 100;
        public IReadOnlyList<TerritoryUnlockConditionInputDto> UnlockConditions { get; set; } = new List<TerritoryUnlockConditionInputDto>();
    }

    /// <summary>
    /// Unlock şartı input (create/update sırasında).
    /// </summary>
    public class TerritoryUnlockConditionInputDto
    {
        public TerritoryUnlockType UnlockType { get; set; }
        public double TargetValue { get; set; }
        public int? RelatedEntityId { get; set; }
        public bool RequiresAll { get; set; } = true;
        public int DisplayOrder { get; set; }
    }
}
