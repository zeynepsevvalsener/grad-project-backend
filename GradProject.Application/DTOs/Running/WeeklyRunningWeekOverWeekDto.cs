namespace GradProject.Application.DTOs.Running
{
    public class WeeklyRunningWeekOverWeekDto
    {
        public int PreviousIsoYear { get; set; }
        public int PreviousIsoWeek { get; set; }
        public DateOnly PreviousWeekStart { get; set; }
        public DateOnly PreviousWeekEnd { get; set; }
        public double PreviousTotalDistanceMeters { get; set; }
        public int PreviousRunCount { get; set; }

        /// <summary>Percent change in total distance vs previous ISO week; positive means more distance.</summary>
        public double? DistanceChangePercent { get; set; }
    }
}
