using GradProject.Application.Interfaces;

namespace GradProject.Infrastructure.Services
{
    public class LanguageResolver : ILanguageResolver
    {
        private const string DefaultLanguage = "en";

        public string ResolveLanguage(string? jwtLang, string? acceptLanguageHeader)
        {
            // 1 JWT claim
            if (!string.IsNullOrWhiteSpace(jwtLang))
                return Normalize(jwtLang);

            // 2️ Accept-Language header
            if (!string.IsNullOrWhiteSpace(acceptLanguageHeader))
            {
                var first = acceptLanguageHeader.Split(',').FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(first))
                    return Normalize(first);
            }

            // 3️⃣ fallback
            return DefaultLanguage;
        }

        private static string Normalize(string value)
        {
            var lang = value.ToLowerInvariant();

            if (lang.StartsWith("tr"))
                return "tr";

            if (lang.StartsWith("en"))
                return "en";

            return DefaultLanguage;
        }
    }
}