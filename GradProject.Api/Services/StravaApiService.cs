using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace GradProject.Api.Services
{
    public class StravaApiService
    {
        private readonly string _accessToken;
        private readonly HttpClient _httpClient;

        public StravaApiService(IConfiguration configuration)
        {
            _accessToken = configuration["STRAVA_ACCESS_TOKEN"];
            _httpClient = new HttpClient();
        }

        public async Task<string> GetAthleteInfo()
        {
            if (string.IsNullOrWhiteSpace(_accessToken))
                return "Hata: Strava access token bulunamadı. Lütfen .env dosyasına ekleyin.";

            var request = new HttpRequestMessage(HttpMethod.Get, "https://www.strava.com/api/v3/athlete");
            request.Headers.Add("Authorization", $"Bearer {_accessToken}");

            var response = await _httpClient.SendAsync(request);
            var data = await response.Content.ReadAsStringAsync();
            return response.IsSuccessStatusCode ? data : $"Hata ({response.StatusCode}): {data}";
        }
    }
}

