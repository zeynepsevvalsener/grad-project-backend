namespace GradProject.Application.DTOs.Gamification
{
    public class JoinChallengeResponseDto
    {
        public int ChallengeId { get; set; }
        
        /// <summary>Progress distance in meters (for Running challenges)</summary>
        public long ProgressDistanceMeters { get; set; }
        
        /// <summary>Progress calories (for Nutrition challenges)</summary>
        public int ProgressCalories { get; set; }
        
        /// <summary>Target value (distance in meters or calories, depending on challenge type)</summary>
        public double TargetValue { get; set; }
        
        public bool Completed { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        /// <summary>
        /// Progress percentage (0-100). Calculated based on challenge metric.
        /// </summary>
        public double ProgressPercent { get; set; }
    }
}

