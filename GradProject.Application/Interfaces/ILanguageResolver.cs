public interface ILanguageResolver
{
    string ResolveLanguage(string? jwtLang, string? acceptLanguageHeader);
}