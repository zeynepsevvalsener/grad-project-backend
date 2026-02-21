using System.Text;
using System.Text.RegularExpressions;

namespace GradProject.Application.Utilities
{
    public static class FoodTextNormalizer
    {
        public static string Normalize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var s = input.Trim().ToLowerInvariant();

            s = s
                .Replace('ı', 'i')
                .Replace('ş', 's')
                .Replace('ğ', 'g')
                .Replace('ü', 'u')
                .Replace('ö', 'o')
                .Replace('ç', 'c');

            var sb = new StringBuilder();

            foreach (var ch in s)
            {
                if (char.IsLetterOrDigit(ch) || ch == ' ')
                    sb.Append(ch);
            }

            // normalize spaces
            var cleaned = Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
            return cleaned;
        }
    }
}