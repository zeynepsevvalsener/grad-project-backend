using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace GradProject.Api.Services
{
    public class StravaApiService
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _redirectUri;
        private readonly HttpClient _httpClient;
        // Geçici kullanıcı/token eşleştirmesi (prod için yerine persistent bir çözüm koymalısınız)
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
                      $"&scope=read&state={Uri.EscapeDataString(userId)}" +
                      $"&approval_prompt=auto";
            return url;
        }

        public async Task<string> ExchangeCodeForTokenAsync(string code, string userId)
        {
            var payload = new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "client_secret", _clientSecret },
                { "code", code },
                { "grant_type", "authorization_code" }
            };
            var response = await _httpClient.PostAsync(
                "https://www.strava.com/oauth/token",
                new FormUrlEncodedContent(payload)
            );

            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                return $"Token alma hatası: {content}";

            var json = JsonDocument.Parse(content).RootElement;
            var token = json.GetProperty("access_token").GetString();
            UserTokens[userId] = token!;
            return token!;
        }

        public string? GetTokenForUser(string userId)
        {
            return UserTokens.TryGetValue(userId, out var token) ? token : null;
        }

        public async Task<string> GetAthleteInfo(string userId)
        {
            var token = GetTokenForUser(userId);
            if (token == null)
                return "Önce yetkilendirme gerekli!";

            var req = new HttpRequestMessage(HttpMethod.Get, "https://www.strava.com/api/v3/athlete");
            req.Headers.Add("Authorization", $"Bearer {token}");
            var res = await _httpClient.SendAsync(req);
            var data = await res.Content.ReadAsStringAsync();
            return res.IsSuccessStatusCode ? data : $"Hata: {data}";
        }
    }
}
