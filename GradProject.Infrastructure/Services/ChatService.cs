using GradProject.Application.DTOs.Chat;
using GradProject.Application.Interfaces;
using GradProject.Application.Interfaces.Gamification;
using GradProject.Application.Interfaces.Nutrition;
using GradProject.Application.Interfaces.Running;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GradProject.Infrastructure.Services
{
    public class ChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _db;
        private readonly IWeeklyRunningSummaryService _weeklySummaryService;
        private readonly IRunningAnalyticsService _analyticsService;
        private readonly IChallengeService _challengeService;
        private readonly IBadgeService _badgeService;
        private readonly IWeeklyNutritionReportService _weeklyNutritionReportService;

        public ChatService(
            HttpClient httpClient,
            AppDbContext db,
            IWeeklyRunningSummaryService weeklySummaryService,
            IRunningAnalyticsService analyticsService,
            IChallengeService challengeService,
            IBadgeService badgeService,
            IWeeklyNutritionReportService weeklyNutritionReportService)
        {
            _httpClient = httpClient;
            _db = db;
            _weeklySummaryService = weeklySummaryService;
            _analyticsService = analyticsService;
            _challengeService = challengeService;
            _badgeService = badgeService;
            _weeklyNutritionReportService = weeklyNutritionReportService;
        }

        public async Task<ChatResponseDto> SendMessageAsync(
            int userId, ChatRequestDto request, CancellationToken ct = default)
        {
            var language = request.Language
                ?? await GetUserLanguageAsync(userId, ct);

            object? context = null;
            if (!string.IsNullOrWhiteSpace(request.RequiresContext))
            {
                context = await BuildContextAsync(userId, request.RequiresContext, ct);
            }

            var payload = new
            {
                userId = userId,
                language = language,
                sessionId = request.SessionId,
                message = request.Message,
                context = context
            };

            AiChatResponse? aiResponse;
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/chat", payload, ct);
                response.EnsureSuccessStatusCode();
                aiResponse = await response.Content.ReadFromJsonAsync<AiChatResponse>(
                    cancellationToken: ct);
            }
            catch
            {
                return new ChatResponseDto
                {
                    SessionId = request.SessionId ?? "",
                    Message = language == "tr"
                        ? "AI servisi şu an ulaşılamıyor, lütfen tekrar dene."
                        : "AI service is unavailable, please try again.",
                    State = "MAIN_MENU"
                };
            }

            if (aiResponse == null)
                return new ChatResponseDto
                {
                    SessionId = request.SessionId ?? "",
                    Message = "No response.",
                    State = "MAIN_MENU"
                };

            return new ChatResponseDto
            {
                SessionId = aiResponse.SessionId,
                Message = aiResponse.Message,
                State = aiResponse.State,
                Data = aiResponse.Data,
                RequiresAction = aiResponse.RequiresAction,
                RequiresContext = aiResponse.RequiresContext
            };
        }

        private async Task<object?> BuildContextAsync(
            int userId, string requiresContext, CancellationToken ct)
        {
            // UTC yerine lokal tarih kullan, timezone farkından kaynaklanan hafta kaymasını önler
            var now = DateTime.Now;
            var isoYear = ISOWeek.GetYear(now);
            var isoWeek = ISOWeek.GetWeekOfYear(now);

            // ISO haftanın Pazartesi'si
            var weekStart = DateOnly.FromDateTime(ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Monday));

            switch (requiresContext)
            {
                case "running":
                    {
                        var summary = await _weeklySummaryService
                            .GetWeeklySummaryAsync(userId, isoYear, isoWeek, true, ct);
                        var analytics = await _analyticsService.GetAnalyticsAsync(userId, ct);

                        // GEÇİCİ DEBUG
                        // Console.WriteLine($"DEBUG running context: runCount={summary.RunCount}, distance={summary.TotalDistanceMeters}, isoYear={isoYear}, isoWeek={isoWeek}");

                        return new
                        {
                            weeklySummary = new
                            {
                                runCount = summary.RunCount,
                                totalDistanceKm = Math.Round(summary.TotalDistanceMeters / 1000, 2),
                                averagePace = summary.AveragePaceMinutesPerKm,
                                totalCaloriesBurned = summary.TotalCaloriesBurned,
                                weekOverWeek = summary.VsPreviousIsoWeek
                            },
                            paceTrend = new
                            {
                                weeklyAveragePace = analytics.PaceTrend.WeeklyAveragePace,
                                overallImprovementPercent = analytics.PaceTrend.OverallImprovementPercentage,
                                last5Runs = analytics.PaceTrend.Last5RunsComparison
                            }
                        };
                    }

                case "challenges":
                    {
                        var challenges = await _challengeService.GetActiveAsync(ct);
                        var summary = await _weeklySummaryService
                            .GetWeeklySummaryAsync(userId, isoYear, isoWeek, false, ct);

                        return new
                        {
                            weeklyDistanceKm = Math.Round(summary.TotalDistanceMeters / 1000, 2),
                            activeChallenges = challenges.Select(c => new
                            {
                                id = c.Id,
                                title = c.Title,
                                description = c.Description,
                                metric = c.MetricName,
                                targetValue = c.TargetValue
                            })
                        };
                    }

                case "badges":
                    {
                        var badges = await _badgeService.GetUserBadgesAsync(userId, ct);
                        return new
                        {
                            earnedBadges = badges.Select(b => new
                            {
                                name = b.Name,
                                description = b.Description,
                                type = b.Type.ToString(),
                                earnedAt = b.EarnedAtUtc
                            })
                        };
                    }

                case "weeklyNutrition":
                    {
                        var report = await _weeklyNutritionReportService
                            .GetWeeklyReportAsync(userId, weekStart, ct);

                        return new
                        {
                            trackedDays = report.TrackedDays,
                            averageScore = report.AverageScore,
                            days = report.Days.Select(d => new
                            {
                                date = d.Date,
                                totalIntakeCalories = d.TotalIntakeCalories,
                                calorieTarget = d.CalorieTarget,
                                dailyScore = d.DailyScore
                            })
                        };
                    }

                default:
                    return null;
            }
        }

        private async Task<string> GetUserLanguageAsync(int userId, CancellationToken ct)
        {
            var lang = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.Language)
                .FirstOrDefaultAsync(ct);

            return string.IsNullOrWhiteSpace(lang) ? "en" : lang;
        }

        private class AiChatResponse
        {
            [JsonPropertyName("sessionId")]
            public string SessionId { get; set; } = "";

            [JsonPropertyName("message")]
            public string Message { get; set; } = "";

            [JsonPropertyName("state")]
            public string State { get; set; } = "";

            [JsonPropertyName("data")]
            public object? Data { get; set; }

            [JsonPropertyName("requiresAction")]
            public string? RequiresAction { get; set; }

            [JsonPropertyName("requiresContext")]
            public string? RequiresContext { get; set; }
        }
    }
}