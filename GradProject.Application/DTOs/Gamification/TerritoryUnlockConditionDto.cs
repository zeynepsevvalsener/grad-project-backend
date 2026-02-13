using GradProject.Domain.Enums;

namespace GradProject.Application.DTOs.Gamification
{
    /// <summary>
    /// Bölge unlock şartı DTO.
    /// </summary>
    public class TerritoryUnlockConditionDto
    {
        public int Id { get; set; }
        public TerritoryUnlockType UnlockType { get; set; }
        public string UnlockTypeName { get; set; } = null!;
        public double TargetValue { get; set; }
        public int? RelatedEntityId { get; set; }
        public bool RequiresAll { get; set; }
    }
}
