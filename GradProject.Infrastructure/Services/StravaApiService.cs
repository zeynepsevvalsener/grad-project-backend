using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace GradProject.Infrastructure.Services
{
    public class StravaApiService
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _redirectUri;
        private readonly HttpClient _httpClient;

        private static readonly ConcurrentDictionary<string, string> UserTokens = new();

        public StravaApiService(IConfiguration configuration)
        {
            _clientId = configuration["STRAVA_CLIENT_ID"];
            _clientSecret = configuration["STRAVA_CLIENT_SECRET"];
            _redirectUri = configuration["STRAVA_REDIRECT_URI"];
            _httpClient = new HttpClient();
        }

        public string GetAuthorizeUrl(string userId)
        {
            var url = $"https://www.strava.com/oauth/authorize" +
                      $"?client_id={_clientId}&response_type=code&redirect_uri={Uri.EscapeDataString(_redirectUri)}" +
                      $"&scope=activity:read&state={Uri.EscapeDataString(userId)}" +
                      $"&approval_prompt=auto";
            return url;
        }

        public async Task<StravaTokenResponse?> ExchangeCodeForTokenAsync(string code)
        {
            var payload = new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "client_secret", _clientSecret },
                { "code", code },
                { "grant_type", "authorization_code" },
                { "redirect_uri", _redirectUri }
            };
            
            try
            {
                var response = await _httpClient.PostAsync(
                    "https://www.strava.com/oauth/token",
                    new FormUrlEncodedContent(payload)
                );

                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    // Log error for debugging
                    Console.WriteLine($"Strava token exchange failed. Status: {response.StatusCode}, Response: {content}");
                    return null;
                }

                var json = JsonDocument.Parse(content).RootElement;
                
                return new StravaTokenResponse
                {
                    AccessToken = json.GetProperty("access_token").GetString()!,
                    RefreshToken = json.TryGetProperty("refresh_token", out var refreshToken) ? refreshToken.GetString() : null,
                    ExpiresAt = json.TryGetProperty("expires_at", out var expiresAt) ? DateTimeOffset.FromUnixTimeSeconds(expiresAt.GetInt64()).DateTime : null,
                    AthleteId = json.TryGetProperty("athlete", out var athlete) && athlete.TryGetProperty("id", out var athleteId) ? athleteId.GetInt64() : null
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during Strava token exchange: {ex.Message}");
                return null;
            }
        }

        public class StravaTokenResponse
        {
            public string AccessToken { get; set; } = null!;
            public string? RefreshToken { get; set; }
            public DateTime? ExpiresAt { get; set; }
            public long? AthleteId { get; set; }
        }

        public string? GetTokenForUser(string userId)
        {
            return UserTokens.TryGetValue(userId, out var token) ? token : null;
        }

        public async Task<string> GetAthleteInfo(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return "Önce yetkilendirme gerekli!";

            var req = new HttpRequestMessage(HttpMethod.Get, "https://www.strava.com/api/v3/athlete");
            req.Headers.Add("Authorization", $"Bearer {accessToken}");
            var res = await _httpClient.SendAsync(req);
            var data = await res.Content.ReadAsStringAsync();
            return res.IsSuccessStatusCode ? data : $"Hata: {data}";
        }

        public async Task<JsonElement?> GetLatestRunActivityAsync(string accessToken)
        {
            try
            {
                // Fetch activities (per_page=1 to get the most recent, then filter for Run type)
                var req = new HttpRequestMessage(HttpMethod.Get, "https://www.strava.com/api/v3/athlete/activities?per_page=30");
                req.Headers.Add("Authorization", $"Bearer {accessToken}");
                
                var res = await _httpClient.SendAsync(req);
                var data = await res.Content.ReadAsStringAsync();

                if (!res.IsSuccessStatusCode)
                {
                    return null;
                }

                var activities = JsonDocument.Parse(data).RootElement;
                
                if (activities.ValueKind != System.Text.Json.JsonValueKind.Array)
                {
                    return null;
                }

                // Find the most recent activity with type "Run"
                foreach (var activity in activities.EnumerateArray())
                {
                    if (activity.TryGetProperty("type", out var typeElement) && 
                        typeElement.GetString()?.Equals("Run", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return activity;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<JsonElement>> GetAllRunActivitiesAsync(string accessToken, int limit = 100)
        {
            try
            {
                // Strava API max per_page is 200, but we'll use 100 as requested
                var perPage = Math.Min(limit, 200);
                var req = new HttpRequestMessage(HttpMethod.Get, $"https://www.strava.com/api/v3/athlete/activities?per_page={perPage}");
                req.Headers.Add("Authorization", $"Bearer {accessToken}");
                
                var res = await _httpClient.SendAsync(req);
                var data = await res.Content.ReadAsStringAsync();

                if (!res.IsSuccessStatusCode)
                {
                    return new List<JsonElement>();
                }

                var activities = JsonDocument.Parse(data).RootElement;
                
                if (activities.ValueKind != System.Text.Json.JsonValueKind.Array)
                {
                    return new List<JsonElement>();
                }

                var runActivities = new List<JsonElement>();
                foreach (var activity in activities.EnumerateArray())
                {
                    if (activity.TryGetProperty("type", out var typeElement) && 
                        typeElement.GetString()?.Equals("Run", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        runActivities.Add(activity);
                        if (runActivities.Count >= limit)
                            break;
                    }
                }

                return runActivities;
            }
            catch
            {
                return new List<JsonElement>();
            }
        }
    }
}

