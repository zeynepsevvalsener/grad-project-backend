using GradProject.Application.Interfaces;

namespace GradProject.Infrastructure.Services
{
    public class LocalizationService : ILocalizationService
    {
        private readonly ICurrentLanguage _currentLanguage;

        public LocalizationService(ICurrentLanguage currentLanguage)
        {
            _currentLanguage = currentLanguage;
        }

        private static readonly Dictionary<string, (string en, string tr)> _messages =
            new()
            {
                ["validation.email.required"] = ("Email is required.", "E-posta zorunludur."),
                ["validation.email.invalid"] = ("Invalid email format.", "Geçersiz e-posta formatı."),
                ["validation.email.maxLength"] = ("Email is too long.", "E-posta çok uzun."),
                ["validation.password.required"] = ("Password is required.", "Şifre zorunludur."),
                ["validation.password.minLength"] = ("Password must be at least 8 characters.", "Şifre en az 8 karakter olmalıdır."),
                ["validation.password.maxLength"] = ("Password is too long.", "Şifre çok uzun."),
                ["auth.invalidCredentials"] = ("Invalid credentials.", "Geçersiz giriş bilgileri.")

            };

        public string Get(string key)
        {
            if (!_messages.TryGetValue(key, out var value))
                return key; // fallback

            return _currentLanguage.Value == "tr"
                ? value.tr
                : value.en;
        }
    }
}