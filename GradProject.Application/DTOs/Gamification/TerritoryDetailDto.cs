namespace GradProject.Application.DTOs.Gamification
{
    /// <summary>
    /// Bölge detay bilgisi (unlock şartları dahil).
    /// </summary>
    public class TerritoryDetailDto : TerritoryResponseDto
    {
        public IReadOnlyList<TerritoryUnlockConditionDto> UnlockConditions { get; set; } = new List<TerritoryUnlockConditionDto>();
    }
}
